using UnityEngine;
using System.Collections;
using System.Collections.Generic;

using System;
using System.Net;
using System.Net.Sockets;

using Google.ProtocolBuffers;



public class NetWorking : MonoBehaviour {
	
	public Material mRed;
	public Material mYellow;
	public Material mBlue;
	public Material mGreen;
	public Material mCyan;
	public Light flaresLight;
	
	private string ip;
	private int port;
	
	private Socket socket;
	private bool connected = false;
	
	private Main MainScript;
	
	
	private int roomId = 0;
	private bool roomOwner = false;
	private Hashtable marines = new Hashtable();
	
	private int groups = 1;
	private System.Random rnd = new System.Random();
	
	private Hashtable mlabelRect = new Hashtable();
	private Hashtable mlabelText = new Hashtable();
	
	private Hashtable marineMaterials = new Hashtable();
	
	private bool showBackButton = false;
	private string backButtonText;

	

	void Awake () {
		if(showBackButton)return; 
		MainScript = GetComponent<Main>();
		
		SetIp(CodeBattle.Static.StaticVar.ip);
		try {
			SetPort(CodeBattle.Static.StaticVar.port);
		}
		catch (Exception e) {
			showBackButton = true;
			backButtonText = e.Message;
			return;
		}
		
		IPAddress ipAddress;
		
		try {
			ipAddress = IPAddress.Parse(ip);
		}
		catch {
			showBackButton = true;
			backButtonText = "Invalid IP Address\nBack";
			return;
		}

		socket = new Socket(
			AddressFamily.InterNetwork,
			SocketType.Stream,
			ProtocolType.Tcp
			);
		
		try {
			socket.Connect(new IPEndPoint(ipAddress, port));
			
			byte[] ReqCreateRoom = AddLengthHeader(CmdCreateRoom(1));
			SockSend(ReqCreateRoom);
			
			ParseMsg(SockRecv());
			connected = true;
		}
		catch (Exception e) {
			Debug.Log(e);
			connected = false;
			showBackButton = true;
			backButtonText = "Network is not available\nBack";
			if(e.Message == "bad msg") {
				backButtonText = "Invalid Message\nBack";
			}
		}

		
		marineMaterials.Add("red", mRed);
		marineMaterials.Add("yellow", mYellow);
		marineMaterials.Add("blue", mBlue);
		marineMaterials.Add("green", mGreen);
		marineMaterials.Add("cyan", mCyan);
	}

	
	// Use this for initialization
	void Start () {
	}
	
	
	void OnGUI() {
		if(showBackButton) {
			if(GUI.Button(new Rect(Screen.width/2 - 100, Screen.height/2 - 30, 200, 60), backButtonText)) {
				Application.LoadLevel("Start");
			}
		}
		GUI.BeginGroup(new Rect(Screen.width/2 - 200/2, 10, 200, 20));
			GUI.Label(new Rect(0, 0, 80, 20), "Room Id");
			GUI.TextField(new Rect(80, 0, 120, 20), roomId.ToString(), 120);
		GUI.EndGroup();

		foreach(DictionaryEntry de in mlabelRect) {
			GUIStyle style = new GUIStyle();
			style.richText = true;

			int hp = (int)mlabelText[de.Key];
			if (hp>=60) style.normal.textColor = Color.green;
			else if(hp>=30) style.normal.textColor = Color.yellow;
			else style.normal.textColor = Color.red;
			
			string hpText = hp.ToString();
			if (hp == 0) hpText = "";
			
			string text = "<size=15><b>" + hpText + "</b></size>";
			GUI.Label((Rect)de.Value, text, style);
		}
		
	}
	
	
	// Update is called once per frame
	void Update () {
		if(!connected) return;
		
		// Wating for Param#1 Microseconds to check is there any data send from server.
		// 1 second == 1000 Millisecond == 1000 * 1000 Microseconds
		if( socket.Poll(10000, SelectMode.SelectRead) ) {
			try {
				ParseMsg(SockRecv());
			}
			catch (Exception e) {
				Debug.Log(e);
				connected = false;
				showBackButton = true;
				backButtonText = "Battle End\nBack";
				return;
			}
		}
	}
	

	
	void SockSend (byte[] data) {
		socket.Send(data, data.Length, SocketFlags.None);
	}
	
	
	byte[] SockRecv () {
		byte[] lenBytes = new byte[4];
		int rec = socket.Receive(lenBytes, 4, SocketFlags.None);
		if (rec == 0) {
			throw new Exception("Remote Closed the connection");
		}
		
		int len =  IPAddress.NetworkToHostOrder( BitConverter.ToInt32(lenBytes, 0) );
		byte[] data = new byte[len];
		rec = socket.Receive(data, len, SocketFlags.None);
		if (rec == 0) {
			throw new Exception("Remote Closed the connection");
		}
		return data;
	}
	
	
	void ParseMsg (byte[] data) {
		CodeBattle.Observer.Message msg;
		try {
			msg = CodeBattle.Observer.Message.ParseFrom(data);
		}
		catch {
			throw new Exception("bad msg");
		}
		// print (msg);
		
		if(msg.Msg == CodeBattle.Observer.MessageEnum.cmdresponse) {
			if(msg.Response.Ret != 0) {
				throw new Exception("Server return an Error Code " + msg.Response.Ret.ToString());
			}
			
			if(msg.Response.Cmd == CodeBattle.Observer.CmdEnum.createroom) {
				roomId = msg.Response.CrmResponse.Id;
				roomOwner = true;
			}
			else if(msg.Response.Cmd == CodeBattle.Observer.CmdEnum.joinroom) {
				roomId = msg.Response.JrmResponse.Id;
			}
		}
		
		if(msg.Msg == CodeBattle.Observer.MessageEnum.createmarine) {
			string c = msg.Marines.Color;
			Material m;
			if (marineMaterials.Contains(c)) m = (Material)marineMaterials[c];
			else m = mRed;
			
			foreach(CodeBattle.Marine marine in msg.Marines.MarineList) {
				Vector3 pos = new Vector3(marine.Position.X, 0, marine.Position.Z);
				GameObject marineInstance = MainScript.CreateOneMarine(pos);
				Marine MarineScript = marineInstance.GetComponent<Marine>();
				MarineScript.hp = marine.Hp;
				MarineScript.id = marine.Id;
				MarineScript.groupId = groups;
				MarineScript.transform.FindChild("MarineLow").renderer.material = m;
				
				marines.Add(marine.Id, MarineScript);
			}
			
			groups++;
		}
		
		if(msg.Msg == CodeBattle.Observer.MessageEnum.senceupdate) {
			// main logic here!
			foreach(CodeBattle.Marine marine in msg.Update.MarineList) {
				OperateMarine(marine);
			}
		}
	}
	
	
	void OperateMarine(CodeBattle.Marine marine) {
		Marine MarineScript = (Marine)marines[marine.Id];
		if (MarineScript.status == CodeBattle.Status.Dead) {
			return;
		};

		MarineScript.status = marine.Status;
		MarineScript.hp = marine.Hp;
		if (marine.Status == CodeBattle.Status.Run) {
			MarineScript.SetTargetPosition(marine.TargetPosition.X, marine.TargetPosition.Z);
		}
		else if (marine.Status == CodeBattle.Status.GunAttack) {
			MarineScript.SetGunShootPosition(marine.TargetPosition.X, marine.TargetPosition.Z);
			CollectionAndReportMarineStates(marine.Id, CodeBattle.Observer.ReportEnum.gunattack);
		}
		else if (marine.Status == CodeBattle.Status.Flares) {
			CollectionAndReportMarineStates(marine.Id, CodeBattle.Observer.ReportEnum.flares);
			StartCoroutine( MarineFlaresReport(marine.Id) );
		}

	}

	
	byte[] AddLengthHeader(byte[] data) {
		byte[] binary = new byte[data.Length + 4];
		int len = IPAddress.HostToNetworkOrder(data.Length);
		byte[] lenBytes = BitConverter.GetBytes(len);
		
		lenBytes.CopyTo(binary, 0);
		data.CopyTo(binary, 4);
		return binary;
	}

	
	byte[] CmdCreateRoom(int MapId) {
		CodeBattle.Observer.CreateRoom.Builder crmBuilder = new CodeBattle.Observer.CreateRoom.Builder();
		crmBuilder.Map = MapId;

		CodeBattle.Observer.Cmd.Builder cmdBuilder = new CodeBattle.Observer.Cmd.Builder();
		cmdBuilder.Cmd_ = CodeBattle.Observer.CmdEnum.createroom;
		
		cmdBuilder.Crm = crmBuilder.BuildPartial();
		CodeBattle.Observer.Cmd cmd = cmdBuilder.BuildPartial();
		return CmdSerialize(cmd);

	}


	byte[] CmdMarineIdleReport(int MarineId, float cx, float cz) {
		CodeBattle.Observer.MarineReport.Builder reportBuilder = new CodeBattle.Observer.MarineReport.Builder();
		reportBuilder.Report = CodeBattle.Observer.ReportEnum.toidle;
		
		CodeBattle.Observer.MarineStatus.Builder msBuilder = new CodeBattle.Observer.MarineStatus.Builder();
		msBuilder.Id = MarineId;
		msBuilder.Status = CodeBattle.Status.Idle;
		
		CodeBattle.Vector2.Builder positionBuilder = new CodeBattle.Vector2.Builder();
		positionBuilder.X = cx;
		positionBuilder.Z = cz;
		
		msBuilder.Position = positionBuilder.BuildPartial();
		
		
		reportBuilder.Midle = msBuilder.BuildPartial();
		
		CodeBattle.Observer.Cmd.Builder cmdBuilder = new CodeBattle.Observer.Cmd.Builder();
		cmdBuilder.Cmd_ = CodeBattle.Observer.CmdEnum.marinereport;
		cmdBuilder.Mrt = reportBuilder.BuildPartial();
		
		CodeBattle.Observer.Cmd cmd = cmdBuilder.BuildPartial();
		return CmdSerialize(cmd);
	}
	
	byte[] CmdMarineDamageReport(int attackId, int damageId) {
		Marine attackMarineScript = (Marine)marines[attackId];
		Marine damageMarineScript = (Marine)marines[damageId];
		
		CodeBattle.Observer.MarineReport.Builder rb = new CodeBattle.Observer.MarineReport.Builder();
		rb.Report = CodeBattle.Observer.ReportEnum.damage;
		
		rb.Mattack = attackMarineScript.GetMarineStatus().BuildPartial();
		rb.Mdamage = damageMarineScript.GetMarineStatus().BuildPartial();
		
		CodeBattle.Observer.Cmd.Builder cmdBuilder = new CodeBattle.Observer.Cmd.Builder();
		cmdBuilder.Cmd_ = CodeBattle.Observer.CmdEnum.marinereport;
		cmdBuilder.Mrt = rb.BuildPartial();
		
		CodeBattle.Observer.Cmd cmd = cmdBuilder.BuildPartial();
		return CmdSerialize(cmd);
	}


	
	byte[] CmdSerialize(CodeBattle.Observer.Cmd cmd) {
		byte[] buffer = new byte[cmd.SerializedSize];
		CodedOutputStream stream = CodedOutputStream.CreateInstance(buffer);
		cmd.WriteTo(stream);
		return buffer;
	}
	
	IEnumerator MarineFlaresReport(int MarineId) {
		flaresLight.intensity = 0.3f;
		yield return new WaitForSeconds(1f);
		flaresLight.intensity = 0.1f;
		CollectionAndReportMarineStates(MarineId, CodeBattle.Observer.ReportEnum.flares2);
	}
	
	
	void CollectionAndReportMarineStates(int ReportId, CodeBattle.Observer.ReportEnum ReportType) {
		CodeBattle.Observer.MarineReport.Builder rb = new CodeBattle.Observer.MarineReport.Builder();
		rb.Report = ReportType;
		rb.ReporterId = ReportId;
		
		foreach(object _m in marines.Values) {
			Marine m = (Marine)(_m);
			if (m.status == CodeBattle.Status.Dead) continue;
			CodeBattle.Observer.MarineStatus.Builder msb = new CodeBattle.Observer.MarineStatus.Builder();
			msb.Id = m.id;
			msb.Status = m.status;
			
			CodeBattle.Vector2.Builder vb = new CodeBattle.Vector2.Builder();
			vb.X = m.transform.position.x;
			vb.Z = m.transform.position.z;
			
			msb.Position = vb.BuildPartial();
			
			rb.AddMarines(msb);
		}
		
		CodeBattle.Observer.Cmd.Builder cmdBuilder = new CodeBattle.Observer.Cmd.Builder();
		cmdBuilder.Cmd_ = CodeBattle.Observer.CmdEnum.marinereport;
		cmdBuilder.Mrt = rb.BuildPartial();
		
		CodeBattle.Observer.Cmd cmd = cmdBuilder.BuildPartial();
		byte[] Data = AddLengthHeader( CmdSerialize(cmd) );
		SockSend(Data);
	}
	


	public void MarineIdleReport(int MarineId, float cx, float cz) {
		if (!connected) return;
		if (!roomOwner) return;
		byte[] cmd = AddLengthHeader(CmdMarineIdleReport(MarineId, cx, cz));
		SockSend(cmd);
	}
	
	public void BulletHitted (int attackId, int damageId) {
		if(!connected) return;
		if(!roomOwner) return;
		
		// Debug.Log("BulletHitted" + attackId + " -> " + damageId);
		byte[] cmd = AddLengthHeader(CmdMarineDamageReport(attackId, damageId));
		SockSend(cmd);
	}
	
	public void UpdateMLabel (int MarineId, Vector3 position, int hp) {
		Vector3 screenPos = Camera.main.WorldToScreenPoint(position);
		int yOffset = 26;
		if (position.z > 25) yOffset = 30;
		Rect rect = new Rect(screenPos.x - 8, Screen.height - screenPos.y - yOffset, 30, 20);
		if (mlabelRect.Contains(MarineId)) {
			mlabelRect[MarineId] = rect;
			mlabelText[MarineId] = hp;
		} else {
			mlabelRect.Add(MarineId, rect);
			mlabelText.Add(MarineId, hp);
		}
	}
	
	public void RemoveMLable (int MarineId) {
		/*
		mlabelRect.Remove(MarineId);
		mlabelText.Remove(MarineId);
		*/
	}
	
	public void SetIp (string Ip) {
		ip = Ip;
	}
	
	public void SetPort (int Port) {
		port = Port;
	}
	
	public void SetPort (string Port) {
		int res;
		if (int.TryParse(Port, out res)) {
			port = res;
		}
		else {
			throw new Exception("Port Should be Integers\nBack");
		}
	}
}

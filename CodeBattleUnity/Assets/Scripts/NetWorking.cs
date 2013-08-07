using UnityEngine;
using System.Collections;
using System.Collections.Generic;

using System;
using System.Net;
using System.Net.Sockets;

using Google.ProtocolBuffers;



public class NetWorking : MonoBehaviour {
	public string ip = "192.168.137.98";
	public int port = 8888;
	
	private Socket socket;
	private bool connected = false;
	
	private Main MainScript;
	
	
	private int roomId;
	private bool roomOwner = false;
	private Hashtable marines = new Hashtable();

	void Awake () {
		MainScript = GetComponent<Main>();
		
		IPAddress ipAddress = IPAddress.Parse(ip);
		socket = new Socket(
			AddressFamily.InterNetwork,
			SocketType.Stream,
			ProtocolType.Tcp
			);
		
		try {
			socket.Connect(new IPEndPoint(ipAddress, port));
			// In my test,  Connect method NEVER thrown an exception even
			// there were wrong ip, port.
			// So, for determine whether we have connected to the server
			// we must do some IO opprate. means send and recv.
			// Actually, There is necessary send data here,
			// For verification or something else
			
			byte[] ReqCreateRoom = AddLengthHeader(CmdCreateRoom(1));
			SockSend(ReqCreateRoom);
			
			ParseMsg(SockRecv());
			connected = true;
		}
		catch (Exception e) {
			print (e);
			connected = false;
			print("NewWorking NOT work!");
		}
	}

	
	// Use this for initialization
	void Start () {

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
				print(e);
				connected = false;
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
		CodeBattle.Api.Message msg = CodeBattle.Api.Message.ParseFrom(data);
		print (msg);
		
		if(msg.Msg == CodeBattle.Api.MessageEnum.cmdresponse) {
			if(msg.Response.Ret != 0) {
				throw new Exception("Server return an Error Code " + msg.Response.Ret.ToString());
			}
			
			if(msg.Response.Cmd == CodeBattle.Api.CmdEnum.createroom) {
				roomId = msg.Response.CrmResponse.Id;
				roomOwner = true;
			}
			/*
			else if(msg.Response.Cmd == CodeBattle.Api.CmdEnum.createmarine) {
				CodeBattle.Marine m = msg.Response.CmeResponse.Marine;

			}
			*/
		}
		
		if(msg.Msg == CodeBattle.Api.MessageEnum.senceupdate) {
			// main logic here!
			foreach(CodeBattle.Marine marine in msg.Update.MarineList) {
				if(marines.Contains(marine.Id)) {
					// operate this marine
					OperateMarine(marine);
					
				} else {
					// create new marine in sence
					GameObject marineInstance = MainScript.CreateOneMarine(
						new Vector3(marine.Position.X, 0, marine.Position.Z)
						);
					Marine MarineScript = marineInstance.GetComponent<Marine>();
					MarineScript.hp = marine.Hp;
					MarineScript.id = marine.Id;
					marines.Add(marine.Id, MarineScript);
				}
			}
		}
	}
	
	
	void OperateMarine(CodeBattle.Marine marine) {
		Marine MarineScript = (Marine)marines[marine.Id];

		MarineScript.status = marine.Status;
		MarineScript.hp = marine.Hp;
		if(marine.HasTargetPosition) {
			MarineScript.SetTargetPosition(marine.TargetPosition.X, marine.TargetPosition.Z);
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
		CodeBattle.Api.CreateRoom.Builder crmBuilder = new CodeBattle.Api.CreateRoom.Builder();
		crmBuilder.Map = MapId;

		CodeBattle.Api.Cmd.Builder cmdBuilder = new CodeBattle.Api.Cmd.Builder();
		cmdBuilder.Cmd_ = CodeBattle.Api.CmdEnum.createroom;
		
		cmdBuilder.Crm = crmBuilder.BuildPartial();
		CodeBattle.Api.Cmd cmd = cmdBuilder.BuildPartial();
		return CmdSerialize(cmd);

	}
	
	/*
	byte[] CmdCreateMarine(string Name, float X, float Z) {
		CodeBattle.Api.CreateMarine.Builder cmeBuilder = new CodeBattle.Api.CreateMarine.Builder();
		cmeBuilder.Roomid = roomId;
		cmeBuilder.Name = Name;
		
		CodeBattle.Vector2.Builder positionBuilder = new CodeBattle.Vector2.Builder();
		positionBuilder.X = X;
		positionBuilder.Z = Z;
		
		cmeBuilder.Position = positionBuilder.BuildPartial();
		
		CodeBattle.Api.Cmd.Builder cmdBuilder = new CodeBattle.Api.Cmd.Builder();
		cmdBuilder.Cmd_ = CodeBattle.Api.CmdEnum.createmarine;
		
		cmdBuilder.Cme = cmeBuilder.BuildPartial();
		CodeBattle.Api.Cmd cmd = cmdBuilder.BuildPartial();
		return CmdSerialize(cmd);
	}
	
	byte[] CmdMarineOperate(int MarineId, CodeBattle.Status Status, float cx, float cz) {
		CodeBattle.Api.MarineOperate.Builder mBuilder = new CodeBattle.Api.MarineOperate.Builder();
		mBuilder.Id = MarineId;
		mBuilder.Status = Status;
		
		CodeBattle.Vector2.Builder currentBuilder = new CodeBattle.Vector2.Builder();
		currentBuilder.X = cx;
		currentBuilder.Z = cz;
		
		mBuilder.CurrentPosition = currentBuilder.BuildPartial();
		return _CmdMarineOperate(mBuilder);
	}
	
	byte[] CmdMarineOperate(int MarineId, CodeBattle.Status Status, float cx, float cz, float tx, float tz) {
		CodeBattle.Api.MarineOperate.Builder mBuilder = new CodeBattle.Api.MarineOperate.Builder();
		mBuilder.Id = MarineId;
		mBuilder.Status = Status;
		
		CodeBattle.Vector2.Builder currentBuilder = new CodeBattle.Vector2.Builder();
		currentBuilder.X = cx;
		currentBuilder.Z = cz;
		
		CodeBattle.Vector2.Builder targetBuilder = new CodeBattle.Vector2.Builder();
		targetBuilder.X = tx;
		targetBuilder.Z = tz;
		
		mBuilder.CurrentPosition = currentBuilder.BuildPartial();
		mBuilder.TargetPostion = targetBuilder.BuildPartial();
		return _CmdMarineOperate(mBuilder);
	}
	
	byte[] CmdMarineOperate(int MarineId, CodeBattle.Status Status, float cx, float cz, int TargetMarineId) {
		CodeBattle.Api.MarineOperate.Builder mBuilder = new CodeBattle.Api.MarineOperate.Builder();
		mBuilder.Id = MarineId;
		mBuilder.Status = Status;
		
		CodeBattle.Vector2.Builder currentBuilder = new CodeBattle.Vector2.Builder();
		currentBuilder.X = cx;
		currentBuilder.Z = cz;
		
		mBuilder.CurrentPosition = currentBuilder.BuildPartial();
		return _CmdMarineOperate(mBuilder);
	}
	
	
	byte[] _CmdMarineOperate(CodeBattle.Api.MarineOperate.Builder mBuilder) {
		CodeBattle.Api.Cmd.Builder cmdBuilder = new CodeBattle.Api.Cmd.Builder();
		

			cmdBuilder.Cmd_ = CodeBattle.Api.CmdEnum.marinereport;
		
		
		cmdBuilder.Opt = mBuilder.BuildPartial();
		CodeBattle.Api.Cmd cmd = cmdBuilder.BuildPartial();
		return CmdSerialize(cmd);
	}
	*/
	
	byte[] CmdSerialize(CodeBattle.Api.Cmd cmd) {
		byte[] buffer = new byte[cmd.SerializedSize];
		CodedOutputStream stream = CodedOutputStream.CreateInstance(buffer);
		cmd.WriteTo(stream);
		return buffer;
	}
	

	/*
	public void BroadcastMarineOperate(int MarineId, CodeBattle.Status Status, float cx, float cz) {
		//if (!ownMarineIds.Contains(MarineId) && !roomOwner) return;
		byte[] cmd = AddLengthHeader(CmdMarineOperate(MarineId, Status, cx, cz));
		SockSend(cmd);
	}
	
	public void BroadcastMarineOperate(int MarineId, CodeBattle.Status Status, float cx, float cz, float tx, float tz) {
		//if (!ownMarineIds.Contains(MarineId) && !roomOwner) return;
		byte[] cmd = AddLengthHeader(CmdMarineOperate(MarineId, Status, cx, cz, tx, tz));
		SockSend(cmd);
	}
	
	public void BroadcastMarineOperate(int MarineId, CodeBattle.Status Status, float cx, float cz, int TargetMarineId) {
		//if (!ownMarineIds.Contains(MarineId) && !roomOwner) return;
		byte[] cmd = AddLengthHeader(CmdMarineOperate(MarineId, Status, cx, cz, TargetMarineId));
		SockSend(cmd);
	}
	*/
}

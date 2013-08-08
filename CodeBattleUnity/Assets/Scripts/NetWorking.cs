using UnityEngine;
using System.Collections;
using System.Collections.Generic;

using System;
using System.Net;
using System.Net.Sockets;

using Google.ProtocolBuffers;



public class NetWorking : MonoBehaviour {
	private string ip = "192.168.137.98";
	private int port = 8887;
	
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
		CodeBattle.Observer.Message msg = CodeBattle.Observer.Message.ParseFrom(data);
		print (msg);
		
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
		
		if(msg.Msg == CodeBattle.Observer.MessageEnum.senceupdate) {
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


	
	byte[] CmdSerialize(CodeBattle.Observer.Cmd cmd) {
		byte[] buffer = new byte[cmd.SerializedSize];
		CodedOutputStream stream = CodedOutputStream.CreateInstance(buffer);
		cmd.WriteTo(stream);
		return buffer;
	}
	


	public void MarineIdleReport(int MarineId, float cx, float cz) {
		if (!roomOwner) return;
		byte[] cmd = AddLengthHeader(CmdMarineIdleReport(MarineId, cx, cz));
		SockSend(cmd);
	}
	
	public void MarineReport(int MarineId, CodeBattle.Status Status, float cx, float cz, float tx, float tz) {
		/*
		if (!roomOwner) return;
		byte[] cmd = AddLengthHeader(CmdMarineReport(MarineId, Status, cx, cz, tx, tz));
		SockSend(cmd);
		*/
	}
}

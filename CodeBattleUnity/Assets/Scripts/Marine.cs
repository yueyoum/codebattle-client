using UnityEngine;
using System.Collections;
using System;

using Status = CodeBattle.Status;

public class Marine : MonoBehaviour {
	public float moveSpeed = 5f;				// moving speed
	
	public Transform RifleEndpoint;
	public GameObject PrefabRifleBullet;
	public GameObject PrefabFlares;
	
	
	private float targetDistance = 0f;
	
	private Vector3 targetPostion;				// where to move
	private Vector3 gunShootPosition;
	

	

	private NetWorking NetWorkingScript;

	
	public Status status = Status.Idle;
	public int hp;
	public int id;
	public int groupId;
	
	private bool dead = false;
	
	void Awake () {
		GameObject SenceMain = GameObject.Find("SenceMain");

		NetWorkingScript = SenceMain.GetComponent<NetWorking>();
		targetPostion = transform.position;
		animation["Run"].speed = 2f;

	}

	// Use this for initialization
	void Start () {

	}


	// Update is called once per frame
	void Update () {
		if (status == Status.Dead) {
			if (dead) return;
			//NetWorkingScript.RemoveMLable(id);
			animation.Play("Die");
			Destroy(gameObject, 2);
			dead = true;
		}
		
		NetWorkingScript.UpdateMLabel(id, transform.position, hp);
		
		if (status == Status.Flares) {
			animation.Play("RifleShoot");
			EmitFlares();
			animation.CrossFade("Idle");
			status = Status.Idle;
			//NewWorkingScript.MarineIdleReport(id, transform.position.x, transform.position.z);
		}
		
		if(status == Status.GunAttack) {
			animation.Play("GunShoot");
			GunShoot();
			animation.CrossFade("Idle");
			status = Status.Idle;
			//NewWorkingScript.MarineIdleReport(id, transform.position.x, transform.position.z);
		}
		
		
		if(status == Status.Run) {
			targetDistance = Vector3.Distance(transform.position, targetPostion);
			if(targetDistance > 0) {
				animation.CrossFade("Run");
				transform.position = Vector3.Lerp(
					transform.position,
					targetPostion,
					Time.deltaTime * moveSpeed/targetDistance
					);
			} else {
				transform.position = targetPostion;
				animation.CrossFade("Idle");
				status = Status.Idle;
				NetWorkingScript.MarineIdleReport(id, transform.position.x, transform.position.z);
			}
		}
 	}
	
	
	void FaceTo(Vector3 target) {
		if (target == transform.position) return;
		Quaternion rot = Quaternion.LookRotation(target - transform.position);
		if (transform.rotation != rot) transform.rotation = rot;
	}
	
	void EmitFlares() {
		Instantiate(
			PrefabFlares,
			RifleEndpoint.position,
			Quaternion.Euler(0, 0, 0)
			);
	}
	
	void GunShoot() {
		Vector3 startPosition = new Vector3(RifleEndpoint.position.x, 1f, RifleEndpoint.position.z);
		
		/*
		Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
		RaycastHit hit;
		Physics.Raycast(ray, out hit, Mathf.Infinity);
		
		FaceTo(hit.point);
		*/
		
		GameObject bullet = Instantiate(
			PrefabRifleBullet,
			startPosition,
			transform.rotation
			) as GameObject;
		
		
		
		RifleBullet rbScript = bullet.GetComponent<RifleBullet>();
		rbScript.targetPosition = gunShootPosition;
		//rbScript.targetPosition = new Vector3(hit.point.x, 1f, hit.point.z);
		rbScript.shooterId = id;
		rbScript.shooterGroupId = groupId;
	}
	
	public void SetTargetPosition(float x, float z) {
		Vector3 p = new Vector3(x, 0, z);
		FaceTo(p);
		targetPostion = p;
	}
	
	public void SetGunShootPosition(float x, float z) {
		Vector3 p = new Vector3(x, 0, z);
		FaceTo(p);
		p.y = 1f;
		gunShootPosition = p;
	}
	
	public CodeBattle.Observer.MarineStatus.Builder GetMarineStatus() {
		CodeBattle.Observer.MarineStatus.Builder b = new CodeBattle.Observer.MarineStatus.Builder();
		b.Id = id;
		b.Status = status;
		
		CodeBattle.Vector2.Builder vb = new CodeBattle.Vector2.Builder();
		vb.X = transform.position.x;
		vb.Z = transform.position.z;
		
		b.Position = vb.BuildPartial();
		return b;
	}
}

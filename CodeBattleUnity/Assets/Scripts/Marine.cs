using UnityEngine;
using System.Collections;
using System;

using Status = CodeBattle.Status;

public class Marine : MonoBehaviour {
	public float moveSpeed = 5f;				// moving speed
	public Projector ShadowProjector;					// shadow projector
	public Projector GunRangeProjector;		// machine gun range projector
	public Projector RifleRangeProjector;				// rifle grenade range projector

	
	public float GunRange = 4f;			// machine gun attack range
	public float RifleRange = 15f;		// rifle grenade attack range
	public float JumpDistance = 10f;
	
	public Transform RifleEndpoint;
	public GameObject PrefabRifleBullet;
	
	
	private float targetDistance = 0f;
	
	private float RifleDamageRange;
	
	private Vector3 targetPostion;				// where to move
	public GameObject attackTarget = null;		// gun attack target
	
	private Main MainScript;
	private NetWorking NewWorkingScript;
	

	private bool selected = false;
	
	
	public Status status = Status.Idle;
	public int hp;
	public int id;
	
	void Awake () {
		GameObject SenceMain = GameObject.Find("SenceMain");
		MainScript = SenceMain.GetComponent<Main>();

		
		NewWorkingScript = SenceMain.GetComponent<NetWorking>();

	}

	// Use this for initialization
	void Start () {
		targetPostion = transform.position;
		
		RifleDamageRange = MainScript.RifleDamageRange;
		
		animation["Run"].speed = 2f;
	}


	// Update is called once per frame
	void Update () {
		if (status == Status.Dead) return;
		
		if(status == Status.GunAttack) {
			animation.CrossFade("GunShoot");
			
			return;

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
				NewWorkingScript.MarineIdleReport(id, transform.position.x, transform.position.z);
			}
		}
 	}
	
	void FaceTo(Vector3 target) {
		if (target == transform.position) return;
		targetPostion = target;
		targetPostion.y = transform.position.y;
		Quaternion rot = Quaternion.LookRotation(targetPostion - transform.position);
		if (transform.rotation != rot) transform.rotation = rot;
	}

	
	public void SetTargetPosition(float x, float z) {
		FaceTo( new Vector3(x, 0, z) );
	}
}

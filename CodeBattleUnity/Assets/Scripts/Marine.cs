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
	

	private float RifleDamageRange;
	
	private Vector3 targetPostion;				// where to move
	public GameObject attackTarget = null;		// gun attack target
	
	private Main MainScript;
	private NetWorking NewWorkingScript;
	private JumpTo JumpToScript;
	

	private bool selected = false;
	
	
	public Status status = Status.Idle;
	public int hp;
	public int id;
	
	//private float GunAttackStartTime = 0f;
	//private float LastGunAttackTime = 0f;
	
	void Awake () {
		GameObject SenceMain = GameObject.Find("SenceMain");
		MainScript = SenceMain.GetComponent<Main>();
		
		/*
		MainScript.OnClick += OnClick;
		MainScript.OnAreaSelection += OnAreaSelection;
		*/
		
		NewWorkingScript = SenceMain.GetComponent<NetWorking>();
		
		JumpToScript = gameObject.GetComponent<JumpTo>();
	}

	// Use this for initialization
	void Start () {
		targetPostion = transform.position;
		
		GunRangeProjector.enabled = false;
		GunRangeProjector.orthographic = true;
		GunRangeProjector.orthoGraphicSize = GunRange;
		
		RifleRangeProjector.enabled = false;
		RifleRangeProjector.orthographic = true;
		RifleRangeProjector.orthoGraphicSize = RifleRange;
		
		RifleDamageRange = MainScript.RifleDamageRange;
		
		//if (gameObject.CompareTag("OwnMarine")) SelectedOn();
		
		animation["Run"].speed = 2f;
	}


	// Update is called once per frame
	void Update () {
		if (status == Status.Dead) return;
		/*
		if (status == Status.RifleAttack || status == Status.Jumping) return;
		if (status == Status.RifleAttackPrepare) {
			Quaternion rot = Quaternion.LookRotation(MainScript.RifleAttackPosition - transform.position);
			if (transform.rotation != rot) {
				transform.rotation = rot;
				MarineChange(Status.RifleAttackPrepare);
			}
		}
		
		
		if(Input.GetKeyDown(KeyCode.Q)) {
			if (!selected || status == Status.RifleAttackPrepare) return;
			RifleModeOn();
		}
		
		if(Input.GetKeyDown(KeyCode.W)) {
			if (!selected) return;
			StartCoroutine(Jump());
		}
		*/
		
		
		// if has attackTarget, detect the distance between self and the target.
		// if distance is less than attack range, then stop moving and start attck.
		// else do nothing, keep moving to the target.
		if(status == Status.GunAttack) {
			animation.CrossFade("GunShoot");
			float dis = Vector3.Distance(transform.position, attackTarget.transform.position);
			if (dis > GunRange) {
				// enemy run away
				//LastGunAttackTime = GunAttackStartTime;
				//GunAttackStartTime = 0f;
				targetPostion = transform.position;
				animation.CrossFade("Idle");
				MarineChange(Status.Idle);
			} else {
				//GunAttackStartTime += Time.deltaTime;
				Quaternion rot = Quaternion.LookRotation(
					attackTarget.transform.position - transform.position
					);
				if (transform.rotation != rot) transform.rotation = rot;
			}
		}
		
		if (status == Status.Run ) {
			RaycastHit _marineForwardHit;
			if(Physics.Raycast(transform.position, transform.forward, out _marineForwardHit, GunRange)) {
				if(_marineForwardHit.transform.gameObject == attackTarget) {
					// got the target, stop moving, face to enemy and shooting
					targetPostion = transform.position;
					Quaternion rot = Quaternion.LookRotation(
						attackTarget.transform.position - transform.position
						);
					if (transform.rotation != rot) transform.rotation = rot;
					animation.CrossFade("GunShootPrepare");
					MarineChange(Status.GunAttack);
				}
			}
		}
		
		if(status == Status.Run) {
			float dis = Vector3.Distance(transform.position, targetPostion);
			if(dis > 0) {
				animation.CrossFade("Run");
				transform.position = Vector3.Lerp(
					transform.position,
					targetPostion,
					Time.deltaTime * moveSpeed/dis
					);
			} else {
				transform.position = targetPostion;
				animation.CrossFade("Idle");
				MarineChange(Status.Idle);
			}
		}
 	}
	
	/*
	IEnumerator Jump() {
		//status = Status.Jumping;
		MarineChange(Status.Jumping);
		Vector3 JumpPostion = transform.position + transform.forward * JumpDistance;
		JumpToScript.targetPosition = JumpPostion;
		JumpToScript.Init();
		yield return StartCoroutine(JumpToScript._Jump());

		targetPostion = transform.position;
		//status = Status.Idle;
		animation.CrossFade("Idle");
		MarineChange(Status.Idle);
	}
	
	
	
	void OnClick(RaycastHit hit, int mouseButton) {
		if (status == Status.Dead || status == Status.RifleAttack || status == Status.Jumping) return;
		
		if (mouseButton == 0) {
			OnClickLeftButton(hit.transform.gameObject);
		}
		else if (mouseButton == 1) {
			OnClickRightButton(hit);
		}
	}
	
	
	void OnClickLeftButton(GameObject target) {
		if (status == Status.RifleAttackPrepare)
			StartCoroutine(RifleAttack(MainScript.RifleAttackPosition));
		else 
			SelectionToggle(target);

	}
	
	void OnClickRightButton(RaycastHit hit) {
		if(!selected) return;
		if(status == Status.RifleAttackPrepare) {
			RifleModeOff();
			return;
		}
		
		// attackTarget = null;
		if(hit.transform.gameObject == gameObject) return;

		
		// Gun Attack Status
		if(status == Status.GunAttack) {
			if (hit.transform.gameObject != attackTarget) {
				// if equal, means player right click the target which he is attacking now
				attackTarget = null;
				animation.CrossFade("Run");
				FaceTo(hit.point);
				MarineChange(Status.Run);
			}
		} else {
			attackTarget = null;
			animation.CrossFade("Run");
			FaceTo(hit.point);
			MarineChange(Status.Run, targetPostion);
		}
		
		if(hit.transform.gameObject.CompareTag("EnemyMarine")) {
			// save the target
			attackTarget = hit.transform.gameObject;
		}
	}
	*/
	
	void FaceTo(Vector3 target) {
		targetPostion = target;
		targetPostion.y = transform.position.y;
		Quaternion rot = Quaternion.LookRotation(targetPostion - transform.position);
		if (transform.rotation != rot) transform.rotation = rot;
	}
	
	/*
	void SelectionToggle(GameObject target) {
		if(target == gameObject && gameObject.CompareTag("OwnMarine")) {
			SelectedOn();
		} else {
			SelectedOff();
		}
	}
	
	void SelectedOn() {
		selected = true;
		GunRangeProjector.enabled = true;
		MainScript.AddSelectedUnit(gameObject);
	}
	
	void SelectedOff() {
		selected = false;
		GunRangeProjector.enabled = false;
	}
	
	
	void OnAreaSelection(Rect rect) {
		if (status == Status.Dead) return;
		Vector3 selfPostion = Camera.main.WorldToScreenPoint(transform.position);
		selfPostion.y = Screen.height - selfPostion.y;
		
		if (gameObject.CompareTag("OwnMarine") && rect.Contains(selfPostion)) {
			SelectedOn();
		}
	}
	
	
	void RifleModeOn() {
		animation.CrossFade("RifleShootPrepare", 0.1f, PlayMode.StopAll);
		//status = Status.RifleAttackPrepare;
		RifleRangeProjector.enabled = true;
		MainScript.RifleDamage.enabled = true;
		GunRangeProjector.enabled = false;
		MarineChange(Status.RifleAttackPrepare);
	}
	
	void RifleModeOff() {
		//if(status == Status.RifleAttackPrepare) {
			animation.CrossFade("Idle");
			MarineChange(Status.Idle);
		//}
		
		RifleRangeProjector.enabled = false;
		MainScript.RifleDamage.enabled = false;
		if(selected) GunRangeProjector.enabled = true;
	}
	
	IEnumerator RifleAttack(Vector3 RifleAttackPosition) {
		float distance = Vector3.Distance(
			RifleAttackPosition, transform.position
			);

		if (distance > RifleRange) {
			print("rifle attack too far");
			
		} else {
			// rifle fire!
			// status = Status.RifleAttack;
			targetPostion = transform.position;
			MarineChange(Status.RifleAttack, RifleAttackPosition);
			yield return new WaitForSeconds(0.5f);
			animation.Play("RifleShoot");

			print ("rfile fire");

			
			GameObject bullet =
			 Instantiate(
				PrefabRifleBullet,
				RifleEndpoint.position,
				transform.rotation
				) as GameObject;
			bullet.GetComponent<RifleBullet>().target = RifleAttackPosition;
			RifleModeOff();
		}
	}
	*/
	
	
	void MarineChange(Status newState) {
		/*
		status = newState;
		if(status == Status.Idle) {
			// no target
			NewWorkingScript.BroadcastMarineOperate(id, status, transform.position.x, transform.position.z);
		}
		else if (status == Status.GunAttack) {
			// target marine id
			NewWorkingScript.BroadcastMarineOperate(
				id, status, transform.position.x, transform.position.z, attackTarget.GetComponent<Marine>().id
				);
		}
		*/
	}
	
	void MarineChange(Status newState, Vector3 targetPosition) {
		/*
		status = newState;
		if(status == Status.Run || status == Status.RifleAttack) {
			// target position
			NewWorkingScript.BroadcastMarineOperate(
				id, status, transform.position.x, transform.position.z, targetPostion.x, targetPostion.z
				);
		}
		else {
			throw new Exception("Error Call in MarineChange");
		}
		*/
	}
	
	public void SetTargetPosition(float x, float z) {
		FaceTo( new Vector3(x, 0, z) );
	}
	
	
	/*
	IEnumerator MarineReport() {
		while(true) {
			if (status == Status.GunAttack) {
				NewWorkingScript.BroadcastMarineOperate(
					id,
					status,
					transform.position.x,
					transform.position.z,
					attackTarget.GetComponent<Marine>().id
					);
			}
			yield return new WaitForSeconds(0.5f);
		}
	}
	*/
}

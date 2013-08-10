using UnityEngine;
using System.Collections;

public class RifleBullet : MonoBehaviour {
	public ParticleEmitter smoke;
	public GameObject explode;
	
	private float moveSpeed = 15;
	
	public Vector3 targetPosition;
	public int shooterId;
	public int shooterGroupId;
	
	private Vector3 finalPosition;
	
	private float targetDistance;
	
	private NetWorking netWorkingScript;
	
	private bool moving = true;
	
	
	

	// Use this for initialization
	void Start () {
		smoke.emit = true;
		netWorkingScript = GameObject.Find("SenceMain").GetComponent<NetWorking>();
		
		Ray ray = new Ray(transform.position, transform.forward);
		RaycastHit hit;
		Physics.Raycast(ray, out hit, Mathf.Infinity, 1 << 9);
		finalPosition = hit.point + ray.direction * 2;

		// Debug.DrawRay(ray.origin, ray.direction*100, Color.green, Mathf.Infinity);
		
	}
	
	// Update is called once per frame
	void Update () {
		if (!moving) return;
		targetDistance = Vector3.Distance(transform.position, finalPosition);
		if(targetDistance > 0f) {
			transform.position = Vector3.Lerp(
				transform.position,
				finalPosition,
				Time.deltaTime * moveSpeed/targetDistance
				);
		} else {
			Destroy(gameObject);
		}
	}
	
	void OnTriggerEnter(Collider other) {
		if (other.gameObject.CompareTag("Marine")) {
			Marine otherMarineScript = other.gameObject.GetComponent<Marine>();
			
			if (shooterGroupId != otherMarineScript.groupId) {			
				print ("BulletHitted");
				explode.transform.FindChild("InnerCore").particleEmitter.emit = true;
				explode.transform.FindChild("Lightsource").light.enabled = true;
				explode.transform.FindChild("OuterCore").particleEmitter.emit = true;
				explode.transform.FindChild("Smoke").particleEmitter.emit = true;
				moving = false;
				renderer.enabled = false;
				
				Destroy(gameObject, 1f);
				
				netWorkingScript.BulletHitted(shooterId, otherMarineScript.id);	
			}
		}
	}
}

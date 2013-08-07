using UnityEngine;
using System.Collections;

public class RifleBullet : MonoBehaviour {
	public ParticleEmitter smoke;
	public Vector3 target = Vector3.zero;
	
	private JumpTo JumpToScript;

	// Use this for initialization
	void Start () {
		//smoke.emit = true;
		JumpToScript = gameObject.GetComponent<JumpTo>();
		JumpToScript.Jump(target);
	}
	
	// Update is called once per frame
	void Update () {
		if(transform.position == target) {
			Destroy(gameObject);
		}	
	}
}

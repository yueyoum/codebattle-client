/* This Script simulate a parabola style movement.
 * It's only support simulate the parabola in XY coordinate,
 * So, what this script actually do is JUMP alone with a parabola path.
 * 
 * targetPosition  - where jump to, this param is used for something like jump to mouse clicked postion.
 * target		   - target GameObject. you can drag any object in unity3d's Inspector panel.
 * 					 when you locate the targetPosition param, target can be leave empty.
 * maxHeight	   - the parabola's max height.
 * 					 it's based on the max height of start position and target position
 * speed		   - jump or move speed. min = 1. more large this value is, moving more fast.
 * spacing		   - sampling for calculate the parabola.
 * 					 more small, the parabola more accurate and smooth.
 * 					 but too small value is meaningless. you can choose 0.1 ~ 2.
 * faceForward	   - if true, the object will rotate itself in jump movement for looking forward.
 * 				   - something like missile.
 */




using UnityEngine;
using System.Collections;

public class JumpTo : MonoBehaviour {
	public Vector3 targetPosition = Vector3.zero;
	public GameObject target = null;
	public float maxHeight = 1f;
	public int speed = 1;
	public float spacing = 0.5f;
	public bool faceForward = true;
	
	private ArrayList Dots = new ArrayList();


	// Use this for initialization
	void Start () {
	}
	
	// Update is called once per frame
	void Update () {
	}
	
	public void Init() {
		Dots.Clear();
		if(targetPosition == Vector3.zero) {
			if(target) targetPosition = target.transform.position;
		}
		
		float initialDistance = Vector3.Distance(targetPosition, transform.position);
		Vector3 middlePoint = Vector3.Lerp(transform.position, targetPosition, 0.5f);
		
		float realMaxHeight = Mathf.Max(targetPosition.y, transform.position.y) + maxHeight;
		Vector3 topPoint = middlePoint;
		topPoint.y += realMaxHeight;
		
		// parabola: x^2 = -2py (p>0)
		// we know start/end point and top point, so, calculate p first.
		// and got the point on parabola via diffents x
		float p = Mathf.Pow(initialDistance/2, 2) / (2 * realMaxHeight);

		Vector3 dir = (targetPosition - transform.position).normalized;
		
		float _h, _dist, _oldDist=initialDistance + 1;
		Vector3 xpoint, ypoint;
		
		// start point to top point part.
		for(int step=1; ; step++) {
			xpoint = middlePoint - dir * spacing * step;
			_h = realMaxHeight - ( Mathf.Pow(spacing * step, 2) / (2 * p) );
			ypoint = xpoint;
			ypoint.y += _h;
			_dist = Vector3.Distance(ypoint, transform.position);
			if(_dist < 0.5f || _oldDist < _dist) break;
			_oldDist = _dist;
			Dots.Add(ypoint);
		}
		
		Dots.Reverse();
		Dots.Insert(0, transform.position);
		_oldDist = initialDistance + 1;
		
		// top point to end point part.
		for(int step=1; ; step++) {
			xpoint = middlePoint + dir * spacing * step;
			_h = realMaxHeight - ( Mathf.Pow(spacing * step, 2) / (2 * p) );
			ypoint = xpoint;
			ypoint.y += _h;
			_dist = Vector3.Distance(ypoint, targetPosition);
			if(_dist < 0.5f || _oldDist < _dist) break;
			_oldDist = _dist;
			Dots.Add(ypoint);
		}
		
		Dots.Add(targetPosition);
		
		// For Debug
		Debug.DrawLine(transform.position, targetPosition, Color.red, 100);
		Debug.DrawLine(middlePoint, topPoint, Color.blue, 100);
		int dotsAmount = Dots.Count;
		Vector3[] dots = (Vector3[])Dots.ToArray(typeof(Vector3));
		for(int index=0; index < dotsAmount -1; index ++) 
			Debug.DrawLine(dots[index], dots[index+1], Color.green, 100);
		
		
		
	}
	
	
	public void Jump() {
		Init();
		StartCoroutine(_Jump());
	}
	
	public void Jump(Vector3 target) {
		targetPosition = target;
		Init();
		StartCoroutine(_Jump());
	}
	
	
	public IEnumerator _Jump() {
		int dotsAmount = Dots.Count;
		Vector3[] dots = (Vector3[])Dots.ToArray(typeof(Vector3));

		int step = 0;
		for(; step < dotsAmount; step+= speed) {
			if(faceForward) transform.LookAt(dots[step]);

			transform.position = dots[step];
			yield return null;
		}
		
		transform.position = targetPosition;
	}
}

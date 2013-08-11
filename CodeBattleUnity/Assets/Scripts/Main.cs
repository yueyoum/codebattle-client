/*
 * 首先在场景中建立一个Empty Object，并且将此脚本附加到建立的Empty Object上
 * 
 * 在Tag管理器中添加 一下 tag：
 * Marine
 * OwnMarine
 * EnemyMarine
 * 
 * 在Layer管理器中添加8号layer： Marine
 * 
 * 在Assets/Resources中建立一个Prefab: PrefabMarine
 * 并将PrefabMarine的层设置为Marine
 * 场景初始化的时候从这个prefab复制出的Marine
 * 
 */



using UnityEngine;
using System.Collections;

public class Main : MonoBehaviour {
	public GameObject PrefabMarine;				// marine prefab
	
	
	/*
	private Vector3 DragStart;
	private Vector3 DragEnd;
	
	private Texture2D _tex;
	private Rect _rect = new Rect(0, 0, 0, 0);
	
	public delegate void DragEvent(Rect rect);
	public event DragEvent OnAreaSelection;
	
	public delegate void ClickEvent(RaycastHit hit, int mouseButton);
	public event ClickEvent OnClick;
	
	private ArrayList selectedUnit = new ArrayList();
	
	private bool hitted;
	*/
	


	void Awake () {
		//CreateMarines();

	}

	// Use this for initialization
	void Start () {
		/*
		PrepareSelectionRectangle();
		
		RifleDamage.enabled = false;
		RifleDamage.orthographic = true;
		RifleDamage.orthoGraphicSize = RifleDamageRange;
		*/
	}
	


	
	// Update is called once per frame
	void Update () {
		/*
		RaycastHit hit;
		hitted = CameraRaycast(out hit);
		if(hitted) {
			RifleDamage.transform.position = new Vector3(
				hit.point.x,
				1,
				hit.point.z
				);
			RifleAttackPosition = RifleDamage.transform.position;
			RifleAttackPosition.y = 0;
		}
		
		
		if(Input.GetMouseButtonDown(0)) {
			DragStart = Input.mousePosition;
			selectedUnit.Clear();

			if(hitted && OnClick != null){
				OnClick(hit, 0);
			}
		}
		
		if(Input.GetMouseButtonDown(1)) {
			if(hitted && OnClick != null) {
				OnClick(hit, 1);
			}
		}
		
		// Draw a rectangle
		if(Input.GetMouseButton(0)) {
			AreaRect();
		}
		
		// hidden the rectangel, and selection logic goes here.
		if(Input.GetMouseButtonUp(0)) {
			if (OnAreaSelection != null)
				OnAreaSelection(_rect);
			_rect = new Rect(0, 0, 0, 0);
		}
		*/
	}
	
	/*
	bool CameraRaycast(out RaycastHit hit) {
		Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
		return Physics.Raycast(ray, out hit, 1000);
	}

	
	void PrepareSelectionRectangle() {
		_tex = new Texture2D(10, 10, TextureFormat.ARGB32, false);
		Color _rectColor = Color.green;
		_rectColor.a = 0.3f;

		for(int x=0; x<10; x++){
			for(int y=0; y<10; y++)  _tex.SetPixel(x, y, _rectColor);
		}
		_tex.wrapMode = TextureWrapMode.Repeat;
		_tex.Apply();
	}
	
	void AreaRect() {
		DragEnd = Input.mousePosition;
		_rect = new Rect(
			Mathf.Min(DragStart.x, DragEnd.x),
			Mathf.Min(Screen.height - DragStart.y, Screen.height - DragEnd.y),
			Mathf.Abs(DragStart.x - DragEnd.x),
			Mathf.Abs(DragStart.y - DragEnd.y)
			);
	}
	
	
	public void AddSelectedUnit(GameObject g) {
		selectedUnit.Add(g);
	}
	
	void CreateMarines() {

		float y = PrefabMarine.transform.position.y;
		CreateOneMarine(new Vector3(0, y, -12), "Marine01", "OwnMarine");
		CreateOneMarine(new Vector3(0, y, 12), "Marine02", "EnemyMarine");

		//CreateOneMarine(Vector3.zero, "MyMarine", "OwnMarine");
	}
	*/
	
	public GameObject CreateOneMarine(Vector3 position) {		
		GameObject marine = Instantiate(
			PrefabMarine,
			position,
			Quaternion.Euler(0, 0, 0)
			) as GameObject;

		return marine;
	}
}

﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using TMPro;
using UnityEngine.EventSystems;


/* @todo {
public class AvgValue<T> {

	T[]	values;
	T zeroValue;
	int size;
	int index = 0;

	public delegate T sum(T a, T b);
	sum sumFunction;
	public delegate T div(T a, int b);
	div divFunction;

	public AvgValue(int size, T zeroValue, sum sumFunction, div divFunction) {
		this.size = size;
		this.zeroValue = zeroValue;
		this.sumFunction = sumFunction;
		this.divFunction = divFunction;
		values = new T[size];
		for (var i = 0; i < size; i++) {
			values[i] = zeroValue;
		}
	}

	public void Add(T value) {
		values[index] = value;
		index = (index + 1) % size;
	}

	public T Get() {
		T sum = zeroValue;
		for (var i = 0; i < size; i++) {
			sum = sumFunction(sum, values[i]);
		}
		
		return divFunction(sum, size);
	}
}
} */

public class RotCylinder : MonoBehaviour {

	public bool withSpace = false;
	public GameObject charPrefab;

	public bool isFixed = false;
	public RotCylinder mainRotCylinder;

	public float minAngularSpeed = 0.03f;

	[HideInInspector] public bool isPaused = false;

	Rigidbody mainRotCylinderRB;

	int nChars { get {
		return 26 + (withSpace ? 1 : 0);
	}}

	//bool isTouching = false;
	RaycastHit lastHit;
	float lastRotationAngle = 0f;
	int rotNumber = 0;

	public int RotNumber { get {
		return rotNumber;
	}}

	/*
	float avgRotationAngle = new AvgValue<float>(3, 0f,
		((float a, float b) =>  (float)(a + b)),
		((float a, int b) => (float)(a / b))
	);
	*/

	public Rigidbody rb;
	
	const int nFramesAvgMomentum = 3;
	int index = 0;
	float[] rotationAngles = new float[nFramesAvgMomentum];
	bool didTouch = false;

	RotCylinder[] rotCylinders;
	List<Rigidbody> rotCylindersRB = new List<Rigidbody>();

	public UnityEvent onRotNumberChange;

    RotCode rotCode;
    List<RaycastResult> raycastResults = new List<RaycastResult>();
    float tPointingUICheck = -1f;
	bool isPointingUI = false;

    public void GenerateChars() {

		// Clear
		#if UNITY_EDITOR
			while (this.transform.childCount > 0) { // ???
				foreach (Transform child in this.transform) {
					Object.DestroyImmediate(child.gameObject, true);
				}
			}
		#else
			foreach (Transform child in this.transform) {
				Destroy(child.gameObject);
			}
		#endif

		// Generate
		var n = 26 + (withSpace ? 1 : 0);
		string charStr;
		GameObject charGameObj;
		for (var i = 0; i < n; i++) {
			charGameObj = Instantiate(/*config.RotCylinderCharPrefab*/charPrefab, transform.position, Quaternion.Euler(-(360f * i / n), 0, 0));
			charGameObj.transform.parent = transform;
			charGameObj.transform.localScale = new Vector3(20f, 1f, 1f);
			if (rotCode.sequence.Length > 0 && mainRotCylinder != this) {
				charStr = rotCode.sequence[i % rotCode.sequence.Length].ToString();
			} else { // ASCII
				if (i < 26) {
					charStr = ((char)(65 + i)).ToString();
				} else {
					charStr = " ";
				}
			}
			charGameObj.GetComponentInChildren<TextMeshPro>().text = charStr;
			charGameObj.name = "Char(" + charStr + ")";
		}
		
	}

	public void Init(RotCode rotCode) {
		this.rotCode = rotCode;
	}

	void Awake() {

		//nChars = 26 + (withSpace ? 1 : 0);
		rb = GetComponent<Rigidbody>();
		mainRotCylinderRB = mainRotCylinder.GetComponent<Rigidbody>();

		for (var i = 0; i < nFramesAvgMomentum; i++) {
			rotationAngles[i] = 0f;
		}

		if (onRotNumberChange == null) {
			onRotNumberChange = new UnityEvent();
		}
		
	}

	void Start () {

		lastHit.point = Vector3.zero;
		rotCylinders = GameObject.FindObjectsOfType<RotCylinder>();
		for (var i = 0; i < rotCylinders.Length; i++) {
			rotCylindersRB.Add(rotCylinders[i].gameObject.GetComponent<Rigidbody>());
		}
	}
	
	void Update () {

		if (isPaused || rotCode == null)
			return;

		if (!isFixed && mainRotCylinder != null &&
			(IsTouching(this) || rb.angularVelocity != Vector3.zero)
		) {
			var rotNum = GetRotNumber();
			if (rotNum != rotNumber) {

				//MyDebug.Log(rotNum);
				rotNumber = rotNum;
				
				if (onRotNumberChange != null)
					onRotNumberChange.Invoke();

				//MyDebug.Log("" + mainRotCylinder.transform.localRotation.eulerAngles + " - " + transform.localRotation.eulerAngles);
				//MyDebug.Log("" + mainRotCylinder.transform.localRotation.eulerAngles + " - " + transform.localRotation.eulerAngles);
			}
		}
		
	}

	IEnumerator RotateQuaternionAnimation(Quaternion to, float duration) {

		float t0 = Time.time;
		var from = transform.rotation;
		float t;
		do {
			t = (Time.time - t0) / duration;
			transform.rotation = Quaternion.Slerp(from, to, t * (2 - t));
			yield return null;
		} while(t < 1);

		yield return null;
	}

	public void SetRotNumber(int rotNumber, bool animate = false, bool stop = true) {

		if (stop) {
			foreach (var rotCylRB in rotCylindersRB) {
				rotCylRB.angularVelocity = Vector3.zero;
			}
		}
		var mainRotAngX = mainRotCylinder.transform.localRotation.eulerAngles.x;
		if (mainRotCylinder.transform.forward.z < 0) {
			mainRotAngX = 180f - mainRotAngX;
		}
		var angleDiff = 360f * rotNumber / nChars;
		var rotV3 = new Vector3(mainRotAngX + angleDiff, 0f, 90f);
		if (animate && this.gameObject.activeInHierarchy) {
			//MyDebug.Log(transform.rotation.eulerAngles);
			//MyDebug.Log(rotV3);
			/*
			if (Mathf.Abs(rotV3.x - transform.eulerAngles.x) > 180f) {
				rotV3.x -= (360f * 4);
			}
			//MyDebug.Log(rotV3);
			// */
			//transform.DORotateQuaternion(Quaternion.Euler(rotV3), 0.4f);
			//transform.DORotate(rotV3, 0.4f);
			StartCoroutine(RotateQuaternionAnimation(Quaternion.Euler(rotV3), 0.4f));
		} else {
			transform.localRotation = Quaternion.Euler(rotV3);
		}
		this.rotNumber = rotNumber;

		if (onRotNumberChange != null)
			onRotNumberChange.Invoke();
	}

	public int GetRotNumber() {

		var mainRot = mainRotCylinder.transform.localRotation;
		var myRot = transform.localRotation;

		/*
		float mainAngle = mainRot.eulerAngles.x;
		float myAngle = myRot.eulerAngles.x;
		// */
		//MyDebug.Log("" + mainAngle + " - " + myAngle);
		// /*
		/*
		if (mainAngle < 0f)
			mainAngle = 360f - mainAngle;
		if (myAngle < 0f)
			myAngle = 360f - myAngle;
		// */

		var angle = Quaternion.Angle(mainRot, myRot);
		if (Vector3.Cross(mainRotCylinder.transform.forward, transform.forward).x < 0f) {
			angle = 360f - angle;
		}

		// */
		//return -(int)((mainAngle - myAngle - 180f / nChars) * nChars / 360f) % nChars;
		return (int)((angle + Mathf.Sign(angle) * (180f / nChars)) * nChars / 360f) % nChars;

	}

	bool IsPointingUI() {
		if (tPointingUICheck + 0.2f > Time.time) {
			return isPointingUI;
		}
		tPointingUICheck = Time.time;
		var pointerData = new PointerEventData(EventSystem.current);
		pointerData.position = Input.mousePosition;
		EventSystem.current.RaycastAll(pointerData, raycastResults);
		isPointingUI = false;
		foreach (var res in raycastResults) {
			if (res.gameObject.layer == 5 && res.gameObject.tag != "IgnoreRaycast") {
				isPointingUI = true;
			}
		}
		return isPointingUI;
	}

	bool IsTouching(RotCylinder rotCyl) {
		return /*rotCode != null && */
			!rotCode.isFixed || rotCyl == null ? rotCode.touchingCyl == rotCyl : rotCode.touchingCyl != null;
	}

    void FixedUpdate() {
		
		if (isPaused || rotCode == null)
			return;

		if (Input.GetMouseButton(0)) {
			Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
			RaycastHit hit;
			Quaternion rot = Quaternion.identity;
			
			if (Physics.Raycast(ray, out hit, 100)) {

				if (hit.transform.gameObject == gameObject &&
					(IsTouching(this) || IsTouching(null) && !IsPointingUI())
				) {
					//isTouching = true;
					rotCode.touchingCyl = this;
					if (isFixed || mainRotCylinderRB.angularVelocity != Vector3.zero) {
						//List<RotCylinder> rotCylinders = new List<RotCylinder>();
						foreach (var rotCylRB in rotCylindersRB) {
							rotCylRB.angularVelocity = Vector3.zero;
						}
						// /*
						foreach (var rotCyl in rotCylinders) {
							if (!rotCyl.isFixed) {
								//rotCyl.SetRotNumber(rotCyl.RotNumber);
								//rotCyl.transform.DOKill();
								rotCyl.StopAllCoroutines();
							}
						}
						// */
					} else {
						rb.angularVelocity = Vector3.zero;
					}
					StopAllCoroutines();
					if (lastHit.point != Vector3.zero) {
						rot = Quaternion.FromToRotation(lastHit.point, hit.point);
						didTouch = true;
					}
					lastHit = hit;
				}
			}
			else {
				//isTouching = false;
				if (IsTouching(this)) {
					rotCode.touchingCyl = null;
				}
			}
			lastRotationAngle = rot.eulerAngles.x;
			lastRotationAngle = (lastRotationAngle > 180) ? lastRotationAngle - 360 : lastRotationAngle;
			rotationAngles[index] = lastRotationAngle;
			rot = Quaternion.Euler(0, -lastRotationAngle, 0);
			if (isFixed) {
				//List<RotCylinder> rotCylinders = new List<RotCylinder>();
				foreach (var rotCyl in rotCylinders) {
					rotCyl.gameObject.transform.localRotation *= rot;
				}
			} else {
				transform.localRotation *= rot;
			}
			index = (index + 1) % nFramesAvgMomentum;
			
		}
		else if (IsTouching(this) || !IsTouching(this) && didTouch) { // Mouse UP (and other)
			var sumRotationAngles = 0f;
			for (var i = 0; i < nFramesAvgMomentum; i++) {
				sumRotationAngles += rotationAngles[i];
				rotationAngles[i] = 0;
			}
			index = 0;
			//isTouching = false;
			rotCode.touchingCyl = null;
			didTouch = false;
			lastHit.point = Vector3.zero;
			var angularVelocity = new Vector3(sumRotationAngles / nFramesAvgMomentum * Mathf.Deg2Rad / Time.deltaTime, 0, 0);

			if (Mathf.Abs(angularVelocity.x) > minAngularSpeed) {
				if (isFixed) {
					foreach (var rotCylRB in rotCylindersRB) {
						rotCylRB.angularVelocity = angularVelocity;
					}
				} else {
					rb.angularVelocity = angularVelocity;
				}
			} else {
				if (isFixed) {
					foreach (var rotCyl in rotCylinders) {
						// /*
						if (!rotCyl.isFixed)
							rotCyl.SetRotNumber(rotCyl.RotNumber, true, false);
						// */
					}
				} else {
					//if (mainRotCylinder.rb.angularVelocity.x == 0)
					SetRotNumber(rotNumber, true, false);
				}
				
			}
			
			//rb.angularVelocity = new Vector3(sumRotationAngles / nFramesAvgMomentum * Mathf.Deg2Rad / Time.deltaTime, 0, 0);
			//isTouching = false;
			rotCode.touchingCyl = null;
		}

		if (rb.angularVelocity != Vector3.zero && Mathf.Abs(rb.angularVelocity.x) < minAngularSpeed) {
			rb.angularVelocity = Vector3.zero;
			if (isFixed) {
				StartCoroutine(FixCylindersOnStop());
			} else {
				if (mainRotCylinderRB.angularVelocity == Vector3.zero) {
					SetRotNumber(rotNumber, true, false);
				}
			}
		}
		
		if (didTouch && rotCode.isFixed && rotCode.touchingCyl != null && rotCode.touchingCyl != this) {
			index = 0;
			didTouch = false;
			lastHit.point = Vector3.zero;
		}
		
	}

	IEnumerator FixCylindersOnStop() {

		yield return new WaitForEndOfFrame();
		var allNotMoving = true;
		foreach (var rotCylRB in rotCylindersRB) {
			if (rotCylRB.angularVelocity != Vector3.zero)
				allNotMoving = false;
		}
		if (allNotMoving) {
			foreach (var rotCyl in rotCylinders) {
				if (!rotCyl.isFixed)
					rotCyl.SetRotNumber(rotCyl.RotNumber, true, false);
			}
		}
	}
}

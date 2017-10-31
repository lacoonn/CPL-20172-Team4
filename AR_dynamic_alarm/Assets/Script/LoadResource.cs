using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LoadResource : MonoBehaviour {
    float x, y, z;
    float time = 0;

    private Transform _parent; // TrackedCamera의 transform 정보 저장
    GameObject _cu; // parent 설정 -> 내 카메라 따라오게 하기 위해 오브젝트로 선언

    // Use this for initialization
    void Start () {
        CreateResource();
    }
	
	// Update is called once per frame
	void Update () {
        /*
        time += Time.deltaTime;
        if(time >= 2)
        {
            x = Random.Range((float)-0.5, (float)0.5);
            y = Random.Range((float)-0.5, (float)0.5);
            z = Random.Range((float)-0.5, (float)0.5);

            GameObject cu = Resources.Load("Interactable") as GameObject;
            _cu = Instantiate(cu) as GameObject;
            _cu.transform.Translate(new Vector3(x, y, z));
            _cu.transform.parent = _parent; // 오브젝트로 선언된 인스턴스의 parent 설정
            time = 0;
        }
        */
	}

    public void CreateResource()
    {
        _parent = (GameObject.Find("TrackedCamera") as GameObject).transform;
        x = Random.Range((float)-0.5, (float)0.5);
        y = Random.Range((float)-0.5, (float)0.5);
        z = Random.Range((float)-0.5, (float)0.5);
        GameObject cu = Resources.Load("Interactable") as GameObject;
        _cu = Instantiate(cu) as GameObject;
        _cu.transform.Translate(new Vector3(x, y, z));
        _cu.transform.parent = _parent; // 오브젝트로 선언된 인스턴스의 parent 설정
    }
}

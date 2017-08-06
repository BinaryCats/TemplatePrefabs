using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlaceHolder : MonoBehaviour {

    public GameObject Prefab;
    private GameObject m_Created;

    public GameObject Created
    {
        get
        {
            return Created;
        }
        set
        {
            m_Created = value;
        }
    }
}

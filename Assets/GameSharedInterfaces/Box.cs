using System;
using UnityEditor;
using UnityEngine;

namespace GameSharedInterfaces
{
    public class Box : MonoBehaviour
    {
        public float XMin => transform.position.x - (transform.localScale.x / 2f);
        public float XMax => transform.position.x + (transform.localScale.x / 2f);

        public float YMin => transform.position.y - (transform.localScale.y / 2f);
        public float YMax => transform.position.y + (transform.localScale.y / 2f);

        public float ZMin => transform.position.z - (transform.localScale.z / 2f);
        public float ZMax => transform.position.z + (transform.localScale.z / 2f);
    }
}
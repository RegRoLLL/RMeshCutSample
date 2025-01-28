using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class Resetter : MonoBehaviour
{
    public void ResetScene()
    {
        SceneManager.LoadScene(this.gameObject.scene.name);
    }
}

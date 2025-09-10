using System.Collections;
using System.Collections.Generic;
using UnityEngine;
#if HAVE_INPUTSYSTEM
using UnityEngine.InputSystem; // New Input System
#endif

namespace Polyperfect.Common
{
    public class Common_KillSwitch : MonoBehaviour
    {

        Animator anim;

        // Use this for initialization
        void Start()
        {

            anim = GetComponent<Animator>();

        }

        // Update is called once per frame
        void Update()
        {
#if HAVE_INPUTSYSTEM
            var kb = Keyboard.current;
            if (kb != null && kb.digit1Key.wasPressedThisFrame)
            {
                anim.SetBool("isDead", true);
            }
#else
            if (Input.GetKeyDown(KeyCode.Alpha1))
            {
                anim.SetBool("isDead", true);
            }
#endif
        }
    }
}

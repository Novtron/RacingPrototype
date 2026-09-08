// RestartHandler.cs
// Wire this to the Restart button's OnClick. Also allows pressing R as a
// keyboard shortcut during play-testing in the Editor.

using UnityEngine;
using UnityEngine.SceneManagement;

namespace RacingPrototype
{
    public class RestartHandler : MonoBehaviour
    {
        [SerializeField] private KeyCode restartKey = KeyCode.R;

        private void Update()
        {
            if (Input.GetKeyDown(restartKey))
            {
                Restart();
            }
        }

        public void Restart()
        {
            Scene current = SceneManager.GetActiveScene();
            SceneManager.LoadScene(current.buildIndex);
        }
    }
}

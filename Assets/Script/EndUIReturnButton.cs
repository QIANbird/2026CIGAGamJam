using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
[RequireComponent(typeof(Button))]
public sealed class EndUIReturnButton : MonoBehaviour
{
    [SerializeField]
    private EndGameFlowController endGameFlowController;

    private Button button;

    private void Awake()
    {
        button = GetComponent<Button>();

        if (endGameFlowController == null)
        {
            endGameFlowController = FindObjectOfType<EndGameFlowController>();
        }
    }

    private void OnEnable()
    {
        if (button == null)
        {
            button = GetComponent<Button>();
        }

        button.onClick.RemoveListener(ReturnToStartMenu);
        button.onClick.AddListener(ReturnToStartMenu);
    }

    private void OnDisable()
    {
        if (button != null)
        {
            button.onClick.RemoveListener(ReturnToStartMenu);
        }
    }

    public void ReturnToStartMenu()
    {
        if (endGameFlowController == null)
        {
            endGameFlowController = FindObjectOfType<EndGameFlowController>();
        }

        if (endGameFlowController == null)
        {
            Debug.LogError(
                "EndUIReturnButton could not find an EndGameFlowController.",
                this);
            return;
        }

        endGameFlowController.ReturnToStartScene();
    }
}

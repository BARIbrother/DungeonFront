using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// UI 버튼을 누를 때 클릭 효과음을 재생한다.
/// ui_click은 모든 클릭에 재생하고, 비활성 버튼에는 ui_deny도 추가로 재생한다.
/// </summary>
[RequireComponent(typeof(Button))]
public sealed class UiButtonSound : MonoBehaviour, IPointerDownHandler, IPointerClickHandler
{
    private static int suppressClickSoundFrame = -1;
    private Button button;
    private bool wasInteractableWhenPressed = true;

    // Button.onClick에서 수락/거절 같은 전용음을 재생한 경우, 같은 클릭의 기본음을 막는다.
    public static void SuppressClickSoundForCurrentFrame()
    {
        suppressClickSoundFrame = Time.frameCount;
    }

    private void Awake()
    {
        button = GetComponent<Button>();
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (eventData.button != PointerEventData.InputButton.Left)
        {
            return;
        }

        // 클릭 처리 도중 성공 로직이 버튼을 비활성화할 수 있으므로,
        // 클릭 완료 시점이 아니라 실제로 누른 순간의 상태를 기억한다.
        wasInteractableWhenPressed = button == null || button.interactable;
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData.button != PointerEventData.InputButton.Left)
        {
            return;
        }

        AudioManager audio = AudioManager.Instance;
        if (audio == null || audio.Catalog == null)
        {
            return;
        }

        if (!wasInteractableWhenPressed)
        {
            audio.PlaySfx(audio.Catalog.uiDeny);
            return;
        }

        if (suppressClickSoundFrame == Time.frameCount)
        {
            return;
        }

        audio.PlaySfx(audio.Catalog.uiClick);
    }
}

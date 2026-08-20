using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// UI 버튼을 누를 때 클릭 효과음을 재생한다.
/// ui_click은 모든 클릭에 재생하고, 비활성 버튼에는 ui_deny도 추가로 재생한다.
/// </summary>
[RequireComponent(typeof(Button))]
public sealed class UiButtonSound : MonoBehaviour, IPointerClickHandler
{
    private static int suppressClickSoundFrame = -1;
    private Button button;

    // Button.onClick에서 수락/거절 같은 전용음을 재생한 경우, 같은 클릭의 기본음을 막는다.
    public static void SuppressClickSoundForCurrentFrame()
    {
        suppressClickSoundFrame = Time.frameCount;
    }

    private void Awake()
    {
        button = GetComponent<Button>();
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

        if (button != null && !button.interactable)
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

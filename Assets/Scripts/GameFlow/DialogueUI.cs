using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;
#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// StoryEventBus 이벤트를 실제 대사 창으로 재생한다. 초상 에셋이 없을 때도 화자 이니셜을 표시하는
/// placeholder를 사용하므로 이벤트와 대화 흐름은 항상 검증할 수 있다.
/// </summary>
public sealed class DialogueUI : MonoBehaviour
{
    private const string PauseRequester = "DialogueUI";
    // P를 1초 동안 누르면 현재 대화를 통째로 닫는다. 대사 중 timeScale=0이므로 unscaled time을 쓴다.
    private const float SkipHoldDuration = 1f;

    private static DialogueUI instance;
    public static event Action<string> OnDialogueClosed;
    private readonly Queue<string> pendingEventIds = new();
    private readonly Dictionary<string, DialogueLine[]> linesByEvent = new()
    {
        ["001E00001"] = new[]
        {
            new DialogueLine("???", "..여보세요, 여보세요?", "unknown", blackout: true),
            new DialogueLine("???", "...이봐요!", "unknown", blackout: true),
            new DialogueLine("주인공", "하암... 누구세요?", "protagonist"),
            new DialogueLine("???", "대장장이 협회 유지 및 관리 지부 소속 4급 감사관 이브입니다.", "eve"),
            new DialogueLine("???", "이 '수제 강철' 대장간은 주인 분께서 은퇴하시면서 공실로 등록되어 있는데, 당신은 누구시길래 이곳을 점유하고 느긋하게 늦잠이나 자고 있는 거죠?", "eve"),
            new DialogueLine("???", "당장 신분을 밝히시죠.", "eve"),
            new DialogueLine("주인공", "아, 감사관 님이셨군요.. 전 여기 전 주인 친척이에요.", "protagonist"),
            new DialogueLine("주인공", "은퇴하신다길래 제가 바로 와서 물려받았죠. 어릴 때부터 쭉 대장장이가 되는 게 꿈이었거든요.", "protagonist"),
            new DialogueLine("주인공", "여기, 제가 있었던 모험가 훈련소 임시 등록증이요.", "protagonist"),
            new DialogueLine("이브", "(등록증을 확인한다)", "eve"),
            new DialogueLine("이브", "흠, 신원은 확실한 것 같군요.", "eve"),
            new DialogueLine("주인공", "그렇죠?", "protagonist"),
            new DialogueLine("이브", "하지만!", "eve"),
            new DialogueLine("주인공", "?", "protagonist"),
            new DialogueLine("이브", "이 등록증만으론 당신이 여길 점유할 권리를 보장하지 못합니다.", "eve"),
            new DialogueLine("이브", "대장장이가 대장간을 운영하려면 협회에서 발급한 '대장장이 면허'가 필요합니다. 그걸 제시해 주시죠.", "eve"),
            new DialogueLine("주인공", "....", "protagonist"),
            new DialogueLine("이브", "빨리요.", "eve"),
            new DialogueLine("주인공", "....", "protagonist"),
            new DialogueLine("이브", "퇴거 조치하겠습니다.", "eve"),
            new DialogueLine("주인공", "잠깐만요! 한 번만, 한 번만 기회를 주세요!", "protagonist"),
            new DialogueLine("이브", "안 됩니다.", "eve"),
            new DialogueLine("주인공", "제발... 아! 그래, 실기 시험이요!", "protagonist"),
            new DialogueLine("주인공", "대장장이 면허 발급받으려면 어차피 실제로 대장간을 운영해 보는 실기 시험이 필요하잖아요?", "protagonist"),
            new DialogueLine("주인공", "지금 그걸 보는 거로 처리해 주세요!", "protagonist"),
            new DialogueLine("이브", "그런 날림 행정을 제가 허용할 거라고 생각합니까?!", "eve"),
            new DialogueLine("이브", "그런 제도가 있기는 하지만, 필기도 보지 않고서..!", "eve"),
            new DialogueLine("주인공", "1번 문제. 다음 중 대장장이 협회에 대한 설명으로 옳지 않은 것은? 보기 1. 대장장이 협회는 모험가 협회와 동등한 지위와 발언권을 가진다. 보기 2. 협회에 소속된 대장장이들은 대장장이 면허를 향시 보유해야 한다. 보기 3. 금 등급 이하의 대장장이들은 자신의 면허를 3년 주기로, 은 등급 이하의 대장장이들은 5년 주기로 갱신해야 한다. 보기 4. 협회는 대장장이에게 특정 물품 생산 또는 생산의 중단, 납품 일정 변경, 대장간 영업 정지 등의 명령을 강제할 권한을 가진다. 정답은 보기 4. 납품 일정 변경은 모험가-대장간 연대 조례 2조 1항에 의해 강제성을 갖지 못 한다.  2번 문제. 다음 중 철 광석의 제련에 대해 옳지 않은 것은? 보기 1. 철 광맥 -> 용광로 -> 기타 제작 장치의 순서로 제작된다. 보기 2. 철 광석의 레벨이 높을수록 높은 레벨의 기계가 필요하며, 기계는 자신의 레벨 이하의 레벨의 철 광석까지 재련할 수 있다. 보기 3. 철 광석은 레벨이 1 오를 때마다 가치가 10배 높아진다. 보기 4. 모든 기계의 제작 재료로는 철 광석에서 파생된 생산품이 필요하다. 정답은 보기 3. 10배가 아니라 5배입니다.", "protagonist", true),
            new DialogueLine("이브", "?!?!?!?!?!?!?!?!", "eve"),
            new DialogueLine("주인공", "필기 시험 문제 정도는 달달 외울 정도로 봤거든요.", "protagonist"),
            new DialogueLine("주인공", "정작 돈이 없어서 시험을 치진 못 했지만..", "protagonist"),
            new DialogueLine("주인공", "이대로 50번까지 전부 말할 수도 있어요.", "protagonist"),
            new DialogueLine("이브", "이건..", "eve"),
            new DialogueLine("이브", "(수첩을 뒤져 본다)", "eve"),
            new DialogueLine("이브", "알겠습니다. 과거에 이런 특례를 인정한 적이 있군요.", "eve"),
            new DialogueLine("이브", "필기는 1개월 후에 치르는 다음 기수를 보십시오.", "eve"),
            new DialogueLine("이브", "그때까지 실기시험을 먼저 치르는 걸로 처리하겠습니다.", "eve"),
            new DialogueLine("주인공", "진짜요?! 헤헤헤.. 고맙습니다! 역시 우리 아름다우신 감사관님께서는", "protagonist"),
            new DialogueLine("이브", "사담은 하지 않겠습니다.", "eve"),
            new DialogueLine("이브", "그리고, 실기에 실패한다면 이 이야기는 없는 겁니다.", "eve"),
            new DialogueLine("이브", "우선 바로 첫 번째 의뢰부터 받아 보시죠.", "eve"),
        },
        ["001E00002"] = new[]
        {
            new DialogueLine("이브", "첫 의뢰는 준비 단계에서 받을 수 있습니다. 의뢰를 수락하고 필요한 물품을 생산하세요.", "eve"),
            new DialogueLine("이브", "생산이 끝나면 결산 단계에서 물품을 한 번에 납품하면 됩니다.", "eve"),
        },
        ["001E00008"] = new[]
        {
            new DialogueLine("주인공", "게임 시작 전, 몇 가지 정보에 대해 알려드릴게요. 우선 하루는 크게 준비-생산-결산 단계로 나눠져 있고, 결산 단계에서 다음 생산 단계로 넘어갈 때 일차수가 1씩 커져요. 참고로, 지금은 준비 단계예요.", "protagonist"),
        },
        ["001E00003"] = new[]
        {
            // 종료 후 [게이트] 이동키 입력 감지 + 5초 대기 → 001E00007로 진행.
            new DialogueLine("주인공", "다음으로는 조작키에 대해 알려드릴게요. 기본적으로 움직일 땐 화살표 방향키, 혹은 WASD를 통해 움직일 수 있어요. 그리고 나중에 기계가 고장나 수리해야 할 때에는 스페이스바를 눌러 수리하면 돼요. 한 번 해보실래요?", "protagonist"),
        },
        ["001E00007"] = new[]
        {
            // 종료 후 [게이트] 채굴기 배치 감지 → 001E00013으로 진행.
            new DialogueLine("주인공", "좋았어요. 다음으로는 기계를 배치해봐요. B키를 눌러 하단의 인벤토리를 열고, 채굴기를 클릭한 뒤 철광석 위에 설치해볼까요?", "protagonist"),
        },
        ["001E00013"] = new[]
        {
            // 종료 후 [게이트] 배치 모드(B키) 닫힘 감지 → 001E00009로 진행.
            new DialogueLine("주인공", "여기까지 순조롭네요. 다음으로 넘어가기 전 B키로 인벤토리를 꺼둘까요?", "protagonist"),
        },
        ["001E00009"] = new[]
        {
            // 종료 후 [게이트] 퀘스트창 열림 감지 → 001E00010으로 진행.
            new DialogueLine("주인공", "다음으로는 대장장이인 만큼, 의뢰를 받아볼 거예요. 의뢰창은, 옆에 있는 QUEST 버튼을 눌러 열 수 있어요. 한 번 열어볼까요?", "protagonist"),
        },
        ["001E00010"] = new[]
        {
            // 종료 후 [게이트] 필수 퀘스트 수락 감지 → 001E00014로 진행.
            new DialogueLine("주인공", "저기 필수 의뢰를 수락해볼까요?", "protagonist"),
        },
        ["001E00014"] = new[]
        {
            new DialogueLine("주인공", "방금 전과 같은 필수 의뢰는 제가 시험을 치기까지 꼭! 무조건! 지켜야 하는 의뢰예요. 이 의뢰를 못 지키면..... 게임 오버인 거죠... 이와 다른 일반 의뢰도 있는데, 이 의뢰는 지키면 보상을 주는 대신, 못 지키면 그만큼의 패널티가 있어요.", "protagonist"),
        },
        ["001E00012"] = new[]
        {
            // 종료 후 [게이트] 퀘스트창 닫힘 감지 → 001E00022로 진행.
            new DialogueLine("주인공", "그럼, 퀘스트창을 닫아볼까요?", "protagonist"),
        },
        ["001E00022"] = new[]
        {
            // 종료 후 [게이트] 레시피북 열림 감지 → 001E00023으로 진행.
            new DialogueLine("주인공", "다음으로는, 지금까지 알려진 레시피들을 모아둔 레시피북이에요. K키를 누르면 언제든 확인할 수 있어요. 한 번 열어볼까요?", "protagonist"),
        },
        ["001E00023"] = new[]
        {
            // 종료 후 [게이트] 레시피북 닫힘 감지 → 001E00015로 진행.
            new DialogueLine("주인공", "레시피북을 닫아볼까요?", "protagonist"),
        },
        ["001E00015"] = new[]
        {
            // 종료 후 [게이트] 테크트리창 열림 감지 → 001E00011로 진행.
            new DialogueLine("주인공", "다음으로는, 앞으로의 공장을 혁신적으로 바꿔줄 테크트리예요. T키, 또는 옆의 테크트리 버튼을 눌러서 테크트리 창을 열어볼까요?", "protagonist"),
        },
        ["001E00011"] = new[]
        {
            new DialogueLine("주인공", "테크트리에서는 앞으로 명성을 통해 다양한 기술/기계들을 해금할 수 있어요.", "protagonist"),
        },
        ["001E00016"] = new[]
        {
            // 종료 후 [게이트] 테크트리창 닫힘 감지 → 001E00017로 진행.
            new DialogueLine("주인공", "테크트리 창을 닫아볼까요?", "protagonist"),
        },
        ["001E00017"] = new[]
        {
            // 종료 후 [게이트] 생산 단계 진입 감지 → (001E00004가 자동으로 뜨고 닫히면) 튜토리얼 패널 표시.
            new DialogueLine("주인공", "그럼, 이제 생산 단계로 넘어가서 직접 생산을 해볼까요?", "protagonist"),
        },
        ["001E00004"] = new[]
        {
            // 종료 후 [게이트] 인벤토리 iron_ore 증가 감지 → 001E00018로 진행.
            // main-Q001-production.md 원문 기반. "채굴기를 배치하자"는 이미 001E00007에서 끝냈으므로
            // "이미 배치해둔 채굴기가 캐고 있다"로 각색했고, e키 안내는 빼고 채굴기 클릭으로 대체했다.
            new DialogueLine("주인공", "자! 철 광석은 어떻게 재련하는지 차근차근 알아 보죠~", "protagonist"),
            new DialogueLine("주인공", "채굴기는 아까 이미 철광석 노드 위에 배치해뒀으니, 지금쯤 부지런히 캐고 있을 거예요.", "protagonist"),
            new DialogueLine("주인공", "그리고 조금만 기다리면..", "protagonist"),
            new DialogueLine("주인공", "나왔어요! 이제 이걸 직접 옮겨야 해요. 나중에는 컨베이어 벨트로 자동화할 수 있겠지만, 지금은 만들 수 없네요..", "protagonist"),
            new DialogueLine("주인공", "채굴기를 클릭해서 인벤토리로 옮겨볼까요?", "protagonist"),
        },
        ["001E00018"] = new[]
        {
            // 종료 후 [게이트] 용광로(Smelter) 배치 감지 → 001E00021로 진행.
            new DialogueLine("주인공", "이제 이 광석을 재련할 용광로가 필요해요. 인벤토리에서 원하시는 곳에 배치하면 돼요.", "protagonist"),
        },
        ["001E00021"] = new[]
        {
            // 종료 후 [게이트] 배치 모드(B키) 닫힘 감지 → 001E00019로 진행.
            new DialogueLine("주인공", "용광로도 놓았으니, B키로 인벤토리를 닫아둘까요? 그래야 용광로를 클릭해서 쓸 수 있거든요.", "protagonist"),
        },
        ["001E00019"] = new[]
        {
            // 종료 후 [게이트] 인벤토리 iron_bar(철 주괴) 증가 감지 → 001E00020으로 진행.
            new DialogueLine("주인공", "이제 여기에 아까 옮겨온 철 광석을 넣으면..", "protagonist"),
            new DialogueLine("주인공", "짠! 이렇게 철 주괴가 나올 거예요! 제 첫 생산품.. 흐흐흐.. 좋아요. 클릭해서 인벤토리에 담아볼까요?", "protagonist"),
        },
        ["001E00020"] = new[]
        {
            // 종료 후 [게이트 없음, 이어서 001E00024 기계 고장 대기 시작] — 001E00017 게이트가 통과된
            // 상태였다면 튜토리얼 패널 표시.
            new DialogueLine("주인공", "좋아요! 이렇게 철 주괴를 생산하는 사이에 또 채굴기가 철 광석을 캤네요. 이제 이 과정을 반복해서 필요한 만큼 만들면 돼요.", "protagonist"),
            new DialogueLine("이브", "...그런데, 왜 저한테 설명하시는 겁니까?", "eve"),
        },
        ["001E00024"] = new[]
        {
            // 종료 후 [게이트] 기계 수리 완료 감지 → 001E00025로 진행.
            new DialogueLine("주인공", "어? 저기 기계 하나가 고장났나 봐요. 가까이 다가가서 스페이스바를 눌러 수리해볼까요?", "protagonist"),
        },
        ["001E00025"] = new[]
        {
            // 종료 후 [게이트 없음] — 수리 튜토리얼 마무리.
            new DialogueLine("주인공", "다 고쳤네요! 이렇게 기계가 고장나면 근처에서 스페이스바로 바로 수리할 수 있으니 잘 기억해두세요.", "protagonist"),
        },
        ["001E00005"] = new[]
        {
            new DialogueLine("주인공", "하아~ 재밌었어요. 감사관님, 여기요! 저희 대장간의 첫 생산품입니다!", "protagonist"),
            new DialogueLine("이브", "흠. 나쁘지 않은 품질이군요. 첫 번째 과제는 합격점을 드리겠습니다.", "eve"),
            new DialogueLine("주인공", "(광기 표정) 다음 과제는 뭔가요? 집행관의 대검? 이터널 플레임 코어? 드래그나이트 마나핵? 뭐든 좋으니까 빨리 주세요!", "protagonist"),
            new DialogueLine("이브", "..그런 S급 아이템을 어떻게 알고 계신지는 모르겠지만, 그런 난이도의 시험을 출제할 리 없잖습니까. 저는 대장간 협회에 당신의 시험 관련 서류를 처리한 후, 모레 다시 오도록 하겠습니다. 그때까지는 대장간 영업을 하셔도 좋습니다. 그럼.", "eve"),
            new DialogueLine("주인공", "... 아쉽다. 뭐, 하나씩 천천히 하다 보면 언젠가는 만들 수 있겠지! 오늘은 이만 쉬자!", "protagonist"),
            new DialogueLine("", "(화면 암전. 오토세이브. 1일차 종료)", "unknown", blackout: true),
        },
        ["001E00006"] = new[]
        {
            new DialogueLine("레이", "마법 기술은 명성이 쌓이면 사용할 수 있습니다. 해금 조건을 확인해 보세요.", "ray"),
        },
    };

    private GameObject modal;
    private GameObject dialogueBox;
    private GameObject blackScreen;
    private TMP_Text speakerText;
    private TMP_Text bodyText;
    private TMP_Text portraitInitialText;
    private TMP_Text blackoutText;
    private TMP_Text nextText;
    private Image portraitImage;
    private Button nextButton;
    // P 홀드 스킵 게이지. 누르는 동안에만 화면 중앙 상단에 표시한다.
    private GameObject skipHoldGauge;
    private Image skipHoldFill;
    private float skipHoldTime;
    // 스킵 직후 같은 누름으로 다음 대화까지 넘어가지 않게 한다.
    private bool skipHoldConsumed;
    private DialogueLine[] activeLines;
    private string activeEventId;
    private int lineIndex;
    private readonly Dictionary<string, Sprite> portraitCache = new();
    private static readonly Color PortraitFallbackColor = new(0.26f, 0.39f, 0.57f, 1f);

    public bool IsShowing => activeLines != null;
    public static bool IsOpen => instance != null && instance.IsShowing;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Bootstrap()
    {
        if (instance != null)
        {
            return;
        }

        var root = new GameObject("DialogueUI");
        root.AddComponent<DialogueUI>();
    }

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        DontDestroyOnLoad(gameObject);
        CreateUi();
    }

    private void OnEnable() => StoryEventBus.OnStoryEvent += HandleStoryEvent;

    private void OnDisable()
    {
        StoryEventBus.OnStoryEvent -= HandleStoryEvent;
        GamePauseService.ReleasePause(PauseRequester);
    }

    private void Update()
    {
        if (Keyboard.current == null)
        {
            skipHoldConsumed = false;
            ResetSkipHoldVisual();
            return;
        }

        if (!Keyboard.current.pKey.isPressed)
        {
            skipHoldConsumed = false;
            ResetSkipHoldVisual();
        }

        if (!IsShowing)
        {
            ResetSkipHoldVisual();
            return;
        }

        if (Keyboard.current.spaceKey.wasPressedThisFrame
            || Keyboard.current.enterKey.wasPressedThisFrame
            || Keyboard.current.numpadEnterKey.wasPressedThisFrame)
        {
            Advance();
            if (!IsShowing)
            {
                return;
            }
        }

        if (!Keyboard.current.pKey.isPressed || skipHoldConsumed)
        {
            return;
        }

        skipHoldTime += Time.unscaledDeltaTime;
        SetSkipHoldProgress(skipHoldTime / SkipHoldDuration);
        if (skipHoldTime >= SkipHoldDuration)
        {
            skipHoldConsumed = true;
            ResetSkipHoldVisual();
            SkipActiveDialogue();
        }
    }

    private void HandleStoryEvent(string eventId)
    {
        if (!linesByEvent.ContainsKey(eventId))
        {
            return;
        }

        pendingEventIds.Enqueue(eventId);
        if (!IsShowing)
        {
            ShowNextPending();
        }
    }

    private void ShowNextPending()
    {
        if (pendingEventIds.Count == 0)
        {
            return;
        }

        activeEventId = pendingEventIds.Dequeue();
        activeLines = linesByEvent[activeEventId];
        lineIndex = 0;
        modal.SetActive(true);
        GamePauseService.RequestPause(PauseRequester);
        ShowLine();
    }

    private void Advance()
    {
        if (!IsShowing)
        {
            return;
        }

        lineIndex++;
        if (lineIndex < activeLines.Length)
        {
            ShowLine();
            return;
        }

        CloseActiveDialogue();
    }

    // 남은 줄을 재생하지 않고 현재 이벤트 대화를 종료한다. 스토리 훅은 정상 종료와 같다.
    private void SkipActiveDialogue()
    {
        if (!IsShowing)
        {
            return;
        }

        CloseActiveDialogue();
    }

    private void CloseActiveDialogue()
    {
        ResetSkipHoldVisual();
        string completedEvent = activeEventId;
        activeEventId = null;
        activeLines = null;
        modal.SetActive(false);
        GamePauseService.ReleasePause(PauseRequester);
        OnDialogueClosed?.Invoke(completedEvent);
        FactoryStoryHooks.NotifyDialogueClosed(completedEvent);
        ShowNextPending();
    }

    private void ShowLine()
    {
        DialogueLine line = activeLines[lineIndex];
        blackScreen.SetActive(line.blackout);
        dialogueBox.SetActive(!line.blackout);
        modal.GetComponent<Image>().color = line.blackout
            ? Color.black
            : new Color(0f, 0f, 0f, 0.62f);

        if (line.blackout)
        {
            blackoutText.text = line.body;
            return;
        }

        speakerText.text = line.speaker;
        bodyText.text = line.body;
        bodyText.enableAutoSizing = true;
        bodyText.fontSizeMin = line.rapid ? 11f : 16f;
        bodyText.fontSizeMax = line.rapid ? 16f : 29f;
        Sprite portrait = LoadPortrait(line.characterId);
        portraitImage.sprite = portrait;
        portraitImage.preserveAspect = true;
        portraitImage.color = portrait != null ? Color.white : PortraitFallbackColor;
        portraitInitialText.gameObject.SetActive(portrait == null);
        if (portrait == null)
        {
            portraitInitialText.text = string.IsNullOrEmpty(line.characterId)
                ? "?"
                : line.characterId.Substring(0, 1).ToUpperInvariant();
        }

        nextText.text = lineIndex == activeLines.Length - 1 ? "닫기" : "다음";
    }

    private void CreateUi()
    {
        EnsureEventSystem();
        var canvasObject = new GameObject("DialogueCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        canvasObject.transform.SetParent(transform, false);
        Canvas canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 2000;
        CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;

        modal = CreatePanel("Modal", canvasObject.transform, new Color(0f, 0f, 0f, 0.62f));
        Stretch(modal.GetComponent<RectTransform>(), Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

        blackScreen = CreatePanel("BlackScreen", modal.transform, Color.black);
        Stretch(blackScreen.GetComponent<RectTransform>(), Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        Button blackoutButton = blackScreen.AddComponent<Button>();
        blackoutButton.transition = Selectable.Transition.None;
        blackoutButton.onClick.AddListener(Advance);
        blackoutText = CreateText("BlackoutText", blackScreen.transform, 36, TextAlignmentOptions.Center, Color.white);
        TmpUiStyle.Apply(blackoutText, TmpUiStyle.Role.Body);
        blackoutText.fontSize = 36f;
        blackoutText.color = Color.white;
        blackoutText.alignment = TextAlignmentOptions.Center;
        blackoutText.textWrappingMode = TextWrappingModes.Normal;
        Stretch(blackoutText.rectTransform, new Vector2(0.12f, 0.38f), new Vector2(0.88f, 0.62f), Vector2.zero, Vector2.zero);
        blackScreen.SetActive(false);

        GameObject box = CreatePanel("DialogueBox", modal.transform, Color.white);
        dialogueBox = box;
        Stretch(box.GetComponent<RectTransform>(), new Vector2(0.06f, 0.03f), new Vector2(0.94f, 0.40f), Vector2.zero, Vector2.zero);
        UiPanelFrame.Apply(box.GetComponent<Image>(), UiPanelFrame.Kind.Wide, 0.42f);

        GameObject portraitFrame = CreatePanel("PortraitFrame", box.transform, Color.white);
        Stretch(portraitFrame.GetComponent<RectTransform>(), new Vector2(0.03f, 0.16f), new Vector2(0.205f, 0.86f), Vector2.zero, Vector2.zero);
        UiPanelFrame.Apply(portraitFrame.GetComponent<Image>(), UiPanelFrame.Kind.Content, 0.65f);

        GameObject portrait = CreatePanel("Portrait", portraitFrame.transform, PortraitFallbackColor);
        Stretch(portrait.GetComponent<RectTransform>(), new Vector2(0.1f, 0.1f), new Vector2(0.9f, 0.9f), Vector2.zero, Vector2.zero);
        portraitImage = portrait.GetComponent<Image>();
        portraitImage.preserveAspect = true;
        portraitInitialText = CreateText("Initial", portrait.transform, 64, TextAlignmentOptions.Center, Color.white);
        Stretch(portraitInitialText.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

        speakerText = CreateText("Speaker", box.transform, 32, TextAlignmentOptions.Left, new Color(1f, 0.82f, 0.38f));
        TmpUiStyle.Apply(speakerText, TmpUiStyle.Role.Title);
        speakerText.fontSize = 32f;
        speakerText.color = new Color(1f, 0.82f, 0.38f);
        Stretch(speakerText.rectTransform, new Vector2(0.23f, 0.70f), new Vector2(0.78f, 0.90f), Vector2.zero, Vector2.zero);
        bodyText = CreateText("Body", box.transform, 26, TextAlignmentOptions.TopLeft, TmpUiStyle.BodyColor);
        TmpUiStyle.Apply(bodyText, TmpUiStyle.Role.Body);
        bodyText.textWrappingMode = TextWrappingModes.Normal;
        bodyText.enableAutoSizing = true;
        bodyText.fontSizeMin = 16f;
        bodyText.fontSizeMax = 26f;
        Stretch(bodyText.rectTransform, new Vector2(0.23f, 0.22f), new Vector2(0.78f, 0.70f), Vector2.zero, Vector2.zero);

        nextButton = CreateButton("NextButton", box.transform, out nextText, "다음");
        Stretch(nextButton.GetComponent<RectTransform>(), new Vector2(0.81f, 0.08f), new Vector2(0.955f, 0.24f), Vector2.zero, Vector2.zero);
        nextButton.onClick.AddListener(Advance);
        CreateSkipHoldGauge(modal.transform);
        modal.SetActive(false);
    }

    // 화면 중앙 상단의 작은 가로 게이지. P를 누르는 동안만 채운다.
    private void CreateSkipHoldGauge(Transform parent)
    {
        skipHoldGauge = CreatePanel("SkipHoldGauge", parent, new Color(0.1f, 0.09f, 0.08f, 0.88f));
        RectTransform root = skipHoldGauge.GetComponent<RectTransform>();
        root.anchorMin = new Vector2(0.5f, 1f);
        root.anchorMax = new Vector2(0.5f, 1f);
        root.pivot = new Vector2(0.5f, 1f);
        root.sizeDelta = new Vector2(220f, 14f);
        root.anchoredPosition = new Vector2(0f, -28f);
        skipHoldGauge.GetComponent<Image>().raycastTarget = false;

        GameObject fillArea = CreatePanel("FillArea", skipHoldGauge.transform, new Color(0f, 0f, 0f, 0f));
        Stretch(fillArea.GetComponent<RectTransform>(), Vector2.zero, Vector2.one, new Vector2(3f, 3f), new Vector2(-3f, -3f));
        fillArea.GetComponent<Image>().raycastTarget = false;

        GameObject fill = CreatePanel("Fill", fillArea.transform, new Color(1f, 0.82f, 0.38f, 1f));
        skipHoldFill = fill.GetComponent<Image>();
        skipHoldFill.raycastTarget = false;
        SetSkipHoldProgress(0f);
        skipHoldGauge.SetActive(false);
    }

    private void SetSkipHoldProgress(float normalized)
    {
        float amount = Mathf.Clamp01(normalized);
        if (skipHoldGauge != null && !skipHoldGauge.activeSelf)
        {
            skipHoldGauge.SetActive(true);
        }

        if (skipHoldFill == null)
        {
            return;
        }

        RectTransform fillRect = skipHoldFill.rectTransform;
        fillRect.anchorMin = Vector2.zero;
        fillRect.anchorMax = new Vector2(amount, 1f);
        fillRect.offsetMin = Vector2.zero;
        fillRect.offsetMax = Vector2.zero;
        skipHoldFill.enabled = amount > 0.001f;
    }

    private void ResetSkipHoldVisual()
    {
        skipHoldTime = 0f;
        if (skipHoldFill != null)
        {
            skipHoldFill.enabled = false;
            RectTransform fillRect = skipHoldFill.rectTransform;
            fillRect.anchorMin = Vector2.zero;
            fillRect.anchorMax = new Vector2(0f, 1f);
            fillRect.offsetMin = Vector2.zero;
            fillRect.offsetMax = Vector2.zero;
        }

        if (skipHoldGauge != null)
        {
            skipHoldGauge.SetActive(false);
        }
    }

    private static GameObject CreatePanel(string name, Transform parent, Color color)
    {
        var panel = new GameObject(name, typeof(RectTransform), typeof(Image));
        panel.transform.SetParent(parent, false);
        panel.GetComponent<Image>().color = color;
        return panel;
    }

    private static TMP_Text CreateText(string name, Transform parent, float fontSize, TextAlignmentOptions alignment, Color color)
    {
        var textObject = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
        textObject.transform.SetParent(parent, false);
        var text = textObject.GetComponent<TextMeshProUGUI>();
        TmpUiStyle.Apply(text, TmpUiStyle.Role.Body);
        text.fontSize = fontSize;
        text.alignment = alignment;
        text.color = color;
        return text;
    }

    private static Button CreateButton(string name, Transform parent, out TMP_Text label, string labelText)
    {
        GameObject buttonObject = CreatePanel(name, parent, Color.white);
        Button button = buttonObject.AddComponent<Button>();
        UiButtonStyle.Apply(button);
        label = CreateText("Label", buttonObject.transform, 22, TextAlignmentOptions.Center, Color.white);
        TmpUiStyle.Apply(label, TmpUiStyle.Role.Button);
        label.fontSize = 22f;
        label.text = labelText;
        Stretch(label.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        return button;
    }

    private static void Stretch(RectTransform rect, Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax)
    {
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.offsetMin = offsetMin;
        rect.offsetMax = offsetMax;
    }

    private static void EnsureEventSystem()
    {
        if (FindAnyObjectByType<EventSystem>() != null)
        {
            return;
        }

        var eventSystem = new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
        DontDestroyOnLoad(eventSystem);
    }

    private Sprite LoadPortrait(string characterId)
    {
        if (string.IsNullOrEmpty(characterId) || characterId == "unknown")
        {
            return null;
        }

        if (portraitCache.TryGetValue(characterId, out Sprite cached))
        {
            return cached;
        }

        string resourcePath = $"Portraits/{characterId}_portrait";
        Sprite sprite = Resources.Load<Sprite>(resourcePath);
        if (sprite == null)
        {
            Texture2D texture = Resources.Load<Texture2D>(resourcePath);
            if (texture != null)
            {
                sprite = Sprite.Create(
                    texture,
                    new Rect(0f, 0f, texture.width, texture.height),
                    new Vector2(0.5f, 0.5f),
                    64f,
                    0u,
                    SpriteMeshType.FullRect);
            }
        }

#if UNITY_EDITOR
        if (sprite == null)
        {
            sprite = AssetDatabase.LoadAssetAtPath<Sprite>(
                $"Assets/Art/UI/Portraits/{characterId}_portrait.png");
        }
#endif

        portraitCache[characterId] = sprite;
        return sprite;
    }

    private readonly struct DialogueLine
    {
        public readonly string speaker;
        public readonly string body;
        public readonly string characterId;
        public readonly bool rapid;
        public readonly bool blackout;

        public DialogueLine(string speaker, string body, string characterId, bool rapid = false, bool blackout = false)
        {
            this.speaker = speaker;
            this.body = body;
            this.characterId = characterId;
            this.rapid = rapid;
            this.blackout = blackout;
        }
    }
}

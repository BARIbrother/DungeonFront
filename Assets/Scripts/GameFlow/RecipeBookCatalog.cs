using System.Collections.Generic;

// 레시피북에 표시할 정적 데이터. 깃허브 Docs/06-recipe.md 기준으로 옮겨 적는다.
public static class RecipeBookCatalog
{
    public sealed class Section
    {
        public readonly string title;
        public readonly List<string> lines;

        public Section(string title, List<string> lines)
        {
            this.title = title;
            this.lines = lines;
        }
    }

    public static readonly List<Section> Sections = new List<Section>
    {
        new Section("나무", new List<string>
        {
            "나무 원목 → 나무 막대기x8",
            "나무 원목 → 종이x4",
        }),
        new Section("철", new List<string>
        {
            "철광석 → 철 주괴",
            "철 주괴 → 철 판",
            "철 주괴 → 철 막대기x2",
            "철 판x4 + 철 주괴x6 → 철 흉갑",
            "철 판x2 + 철 주괴x4 → 철 투구",
            "철 판x2 + 철 주괴x5 → 철 각반",
            "철 판x2 + 철 주괴x2 → 철 부츠",
            "철 막대기x2 + 나무 막대기x2 → 철 검",
            "철 판x4 + 철 막대기x2 + 나무 막대기x2 → 철 전투 망치",
        }),
        new Section("기초 마력석", new List<string>
        {
            "마력석 광석 → 마력석 결정",
            "마력석 결정 + 철 주괴 → 마력 코어",
            "마력 코어 + 나무 막대기x2 → 마나 완드",
            "철 검 + 마력 코어x2 → 마나강 검",
            "철 투구 + 마력 코어x2 → 마나강 투구",
            "철 흉갑 + 마력 코어x4 → 마나강 흉갑",
            "철 각반 + 마력 코어x3 → 마나강 각반",
            "철 부츠 + 마력 코어x2 → 마나강 부츠",
        }),
        new Section("마나 포집", new List<string>
        {
            "(없음) → 하급 마나 정수",
            "(없음) → 중급 마나 정수",
            "(없음) → 상급 마나 정수",
            "(없음) → 던전의 주인의 정수",
        }),
        new Section("마법 스크롤", new List<string>
        {
            "종이 + 상급 마나 정수 → 빈 마법 스크롤",
            "빈 마법 스크롤 + 상급 마나 정수x3 → 원소 스크롤",
            "원소 스크롤 + 상급 마나 정수 → 원소 형태 스크롤",
            "빈 마법 스크롤 + 마력 코어 + 상급 마나 정수 → 빈 2단계 마법 스크롤",
            "빈 2단계 마법 스크롤 + 던전의 주인의 정수 → 2단계 원소 스크롤",
            "2단계 원소 스크롤 + 상급 마나 정수x2 + 중급 마나 정수 → 2단계 원소 형태 스크롤",
        }),
        new Section("의식", new List<string>
        {
            "빈 2단계 마법 스크롤 + 던전의 주인의 정수x2 → 의식 스크롤",
            "의식 스크롤 + 철 검x10 + 흑강 검x10 + 백강 검x10 + 진강 검x10 → 검의 의식",
            "검의 의식x10 → 전쟁의 의식",
            "의식용 철제 대검 + 검의 의식 → 집행자의 대검",
            "집행자의 대검 + 전쟁의 의식 → 전쟁에 물든 집행자의 대검",
        }),
        new Section("칠흑석", new List<string>
        {
            "칠흑석 광석 → 칠흑석 주괴",
            "철 주괴 + 칠흑석 주괴 → 흑강 주괴",
            "흑강 주괴 → 흑강 판",
            "흑강 주괴 → 흑강 막대기x2",
            "철 검 + 흑강 주괴x2 + 흑강 막대기x2 → 흑강 검",
            "철 전투 망치 + 흑강 판x4 + 흑강 막대기x2 → 흑강 전투 망치",
            "철 투구 + 흑강 판x3 → 흑강 투구",
            "철 흉갑 + 흑강 판x6 → 흑강 흉갑",
            "철 각반 + 흑강 판x5 → 흑강 각반",
            "철 부츠 + 흑강 판x2 → 흑강 부츠",
        }),
        new Section("순백석", new List<string>
        {
            "순백석 광석 → 순백석 주괴",
            "철 주괴 + 순백석 주괴 → 백강 주괴",
            "백강 주괴 → 백강 판",
            "백강 주괴 → 백강 막대기x2",
            "철 검 + 백강 주괴x2 + 백강 막대기x2 → 백강 검",
            "철 전투 망치 + 백강 판x4 + 백강 막대기x2 → 백강 전투 망치",
            "철 투구 + 백강 판x3 → 백강 투구",
            "철 흉갑 + 백강 판x6 → 백강 흉갑",
            "철 각반 + 백강 판x5 → 백강 각반",
            "철 부츠 + 백강 판x2 → 백강 부츠",
        }),
        new Section("고급 마법", new List<string>
        {
            "마나강 흉갑 + 마나강 각반 + 던전의 주인의 정수x3 → 마술사의 로브",
            "마력 코어 lv2 + 칠흑석 주괴 lv2 → 흑마법 코어",
            "마술사의 로브 + 흑마법 코어 → 흑마술사의 로브",
            "마나 완드 + 흑마법 코어 → 흑마술 지팡이",
            "마력 코어 lv2 + 순백석 주괴 lv2 → 백마법 코어",
            "마술사의 로브 + 백마법 코어 → 백마술사의 로브",
            "마나 완드 + 백마법 코어 → 백마술 지팡이",
        }),
        new Section("진강", new List<string>
        {
            "철 주괴 lv2 + 칠흑석 주괴 lv2 + 순백석 주괴 lv2 → 진강 주괴",
            "진강 주괴 → 진강 판",
            "진강 주괴 → 진강 막대기x2",
            "흑강 검 + 백강 검 + 진강 막대기x2 → 진강 검",
            "흑강 전투 망치 + 백강 전투 망치 + 진강 막대기x2 → 진강 전투 망치",
            "흑강 투구 + 백강 투구 + 진강 판x1 → 진강 투구",
            "흑강 흉갑 + 백강 흉갑 + 진강 판x2 → 진강 흉갑",
            "흑강 각반 + 백강 각반 + 진강 판x2 → 진강 각반",
            "흑강 부츠 + 백강 부츠 + 진강 판x1 → 진강 부츠",
        }),
        new Section("건축 트리", new List<string>
        {
            "돌 → 콘크리트",
            "철 주괴x10 + 철 막대기x20 → 철제 기둥 뼈대",
            "철 주괴x10 + 철 막대기x20 → 철제 대들보 뼈대",
            "철 주괴x20 + 철 막대기x50 → 철제 지붕 뼈대",
            "철제 지붕 뼈대 + 콘크리트x150 → 구조물 지붕",
            "철제 대들보 뼈대 + 콘크리트x50 → 구조물 대들보",
            "철제 기둥 뼈대 + 콘크리트x50 → 구조물 기둥",
            "구조물 기둥x8 + 구조물 대들보x2 + 구조물 지붕 → 제단",
            "철 판 lv3x20 + 철 막대기 lv3x10 → 대검 날",
            "대검 날 + 철 막대기 lv3x10 + 철 주괴 lv3x10 → 의식용 철제 대검",
        }),
    };

    // 레시피북이 실제로 펼치는 페이지 배치. 자동 계산이 아니라 가독성 기준으로 직접 정한 값이다.
    // (섹션 이름, 그 섹션에서 시작 인덱스, 이 페이지에 넣을 개수).
    // start가 0이면 그 섹션의 시작이라 제목이 보이고, 0이 아니면 이전 페이지에서 이어지는
    // 조각이라 제목 자리를 비워 둔 채(높이는 유지) 렌더링된다.
    public readonly struct PageChunk
    {
        public readonly string sectionTitle;
        public readonly int start;
        public readonly int count;

        public PageChunk(string sectionTitle, int start, int count)
        {
            this.sectionTitle = sectionTitle;
            this.start = start;
            this.count = count;
        }
    }

    public static readonly PageChunk[][] PageLayout =
    {
        // 1페이지: 나무 전체 + 철 앞부분(주괴/판/막대기)
        new[] { new PageChunk("나무", 0, 2), new PageChunk("철", 0, 3) },
        // 2페이지: 철 나머지(이어서 — 흉갑/투구/각반/부츠/검/전투 망치)
        new[] { new PageChunk("철", 3, 6) },
        // 3페이지: 기초 마력석 앞부분(결정/코어/완드/마나강검/투구/흉갑)
        new[] { new PageChunk("기초 마력석", 0, 6) },
        // 4페이지: 기초 마력석 나머지(이어서 — 각반/부츠) + 마나 포집 전체
        new[] { new PageChunk("기초 마력석", 6, 2), new PageChunk("마나 포집", 0, 4) },
        // 5페이지: 마법 스크롤 전체
        new[] { new PageChunk("마법 스크롤", 0, 6) },
        // 6페이지: 의식 전체
        new[] { new PageChunk("의식", 0, 5) },
        // 7페이지: 칠흑석 전체 (촘촘하게)
        new[] { new PageChunk("칠흑석", 0, 10) },
        // 8페이지: 순백석 전체 (촘촘하게)
        new[] { new PageChunk("순백석", 0, 10) },
        // 9페이지: 고급 마법 전체
        new[] { new PageChunk("고급 마법", 0, 7) },
        // 10페이지: 진강 전체 (촘촘하게)
        new[] { new PageChunk("진강", 0, 9) },
        // 11페이지: 건축 트리 앞부분
        new[] { new PageChunk("건축 트리", 0, 5) },
        // 12페이지: 건축 트리 나머지(이어서)
        new[] { new PageChunk("건축 트리", 5, 5) },
    };

    // 위 PageLayout 중 레시피 사이 간격을 좁게(촘촘하게) 보여줄 페이지의 0-based 인덱스.
    public static readonly HashSet<int> CompactPageIndices = new HashSet<int> { 6, 7, 9 };
}

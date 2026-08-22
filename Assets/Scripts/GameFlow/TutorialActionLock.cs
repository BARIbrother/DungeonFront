using UnityEngine;

// 1일차 스토리 튜토리얼 동안, 현재 게이트가 지시하는 행동만 허용한다.
// F8로 제한을 전부 해제한다.
public static class TutorialActionLock
{
    public enum Gate
    {
        None,
        WaitMovementInput,
        WaitMinerPlacement,
        WaitPlacementModeClose,
        WaitQuestWindowOpen,
        WaitMandatoryQuestAccept,
        WaitQuestWindowClose,
        WaitRecipeBookOpen,
        WaitRecipeBookClose,
        WaitTechTreeOpen,
        WaitTechTreeClose,
        WaitProductionStarted,
        WaitOreCollected,
        WaitSmelterPlacement,
        WaitIronIngotCollected,
        WaitMachineBroken,
        WaitMachineRepaired,
    }

    public enum Action
    {
        Move,
        OpenPlacement,
        ClosePlacement,
        PlaceMiner,
        PlaceSmelter,
        PlaceOtherMachine,
        PickupMachine,
        OpenQuest,
        CloseQuest,
        AcceptQuest,
        OpenRecipeBook,
        CloseRecipeBook,
        OpenTechTree,
        CloseTechTree,
        UnlockTech,
        StartProduction,
        OpenInventory,
        OpenShop,
        OpenZoneUnlock,
        OpenMachineCraft,
        ForceEndProduction,
        Repair,
        InteractMachine,
    }

    private static bool tutorialActive;
    private static bool restrictionsReleased;
    private static Gate currentGate;

    public static bool IsRestricting => tutorialActive && !restrictionsReleased;

    public static void SetTutorialActive(bool active)
    {
        tutorialActive = active;
        if (!active)
        {
            currentGate = Gate.None;
        }
    }

    public static void SetGate(Gate gate)
    {
        currentGate = gate;
    }

    public static void ReleaseAll()
    {
        restrictionsReleased = true;
        Debug.Log("[TutorialActionLock] F8 — 튜토리얼 행동 제한을 해제했습니다.");
    }

    public static void Reset()
    {
        tutorialActive = false;
        restrictionsReleased = false;
        currentGate = Gate.None;
    }

    public static bool Allows(Action action)
    {
        if (!IsRestricting)
        {
            return true;
        }

        switch (currentGate)
        {
            case Gate.WaitMovementInput:
                return action == Action.Move;

            case Gate.WaitMinerPlacement:
                return action == Action.Move
                    || action == Action.OpenPlacement
                    || action == Action.PlaceMiner;

            case Gate.WaitPlacementModeClose:
                return action == Action.ClosePlacement;

            case Gate.WaitQuestWindowOpen:
                return action == Action.OpenQuest;

            case Gate.WaitMandatoryQuestAccept:
                return action == Action.AcceptQuest;

            case Gate.WaitQuestWindowClose:
                return action == Action.CloseQuest;

            case Gate.WaitRecipeBookOpen:
                return action == Action.OpenRecipeBook;

            case Gate.WaitRecipeBookClose:
                return action == Action.CloseRecipeBook;

            case Gate.WaitTechTreeOpen:
                return action == Action.OpenTechTree;

            case Gate.WaitTechTreeClose:
                return action == Action.CloseTechTree;

            case Gate.WaitProductionStarted:
                return action == Action.StartProduction;

            case Gate.WaitOreCollected:
                return action == Action.Move || action == Action.InteractMachine;

            case Gate.WaitSmelterPlacement:
                return action == Action.Move
                    || action == Action.OpenPlacement
                    || action == Action.PlaceSmelter;

            case Gate.WaitIronIngotCollected:
                return action == Action.Move || action == Action.InteractMachine;

            case Gate.WaitMachineBroken:
                return action == Action.Move;

            case Gate.WaitMachineRepaired:
                return action == Action.Move || action == Action.Repair;

            default:
                return false;
        }
    }

    public static bool AllowsPlacementOf(string machineTypeId)
    {
        if (string.IsNullOrEmpty(machineTypeId))
        {
            return Allows(Action.PlaceOtherMachine);
        }

        if (machineTypeId.StartsWith("Miner", System.StringComparison.Ordinal))
        {
            return Allows(Action.PlaceMiner);
        }

        if (machineTypeId.StartsWith("Smelter", System.StringComparison.Ordinal))
        {
            return Allows(Action.PlaceSmelter);
        }

        return Allows(Action.PlaceOtherMachine);
    }
}

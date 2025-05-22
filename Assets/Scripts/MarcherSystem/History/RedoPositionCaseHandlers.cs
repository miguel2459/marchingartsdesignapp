using UnityEngine;

public static class RedoPositionCaseHandlers
{
    private static MarcherPositionService marcherService;
    private static EnsembleDirector2 director;

    public static void Initialize(MarcherPositionService service, EnsembleDirector2 dir)
    {
        marcherService = service;
        director = dir;
    }

    public static void HandleConfirmedMoveOnConfirmed(MarcherPositionsManager marcher, int set, int count, PositionEntry after)
    {
        marcherService.ConfirmFromUndo(marcher, set, count, after.pos);
        marcher.GetComponent<MarcherVisualStateController>()?.SetSelectorVisible(true);
        director?.VisualizePathsForSet(set);
    }

    public static void HandleConfirmedMoveOnInferred(MarcherPositionsManager marcher, int set, int count, PositionEntry after)
    {
        marcherService.ConfirmFromUndo(marcher, set, count, after.pos);
        marcher.GetComponent<MarcherVisualStateController>()?.SetSelectorVisible(false);
        director?.VisualizePathsForSet(set);
    }

    public static void HandleConfirmedMoveOnRaw(MarcherPositionsManager marcher, int set, int count, PositionEntry after)
    {
        marcherService.ConfirmFromUndo(marcher, set, count, after.pos);
        marcher.GetComponent<MarcherVisualStateController>()?.UpdateSelectorAnchor(set, count);
        director?.VisualizePathsForSet(set);
        director?.RefreshDashedPreviewForSelected();
    }

    public static void HandleConfirmedDeleteOnConfirmed(MarcherPositionsManager marcher, int set, int count, PositionEntry after)
    {
        marcherService.DeleteFromUndo(marcher, set, count, out _);
    }

    public static void HandleConfirmedDeleteOnInferred(MarcherPositionsManager marcher, int set, int count, PositionEntry after)
    {
        marcherService.DeleteFromUndo(marcher, set, count, out _);
        marcher.GetComponent<MarcherVisualStateController>()?.UpdateSelectorAnchor(set, count);
        marcher.transform.position = after.pos;
    }

    public static void HandleConfirmedDeleteOnRaw(MarcherPositionsManager marcher, int set, int count, PositionEntry after)
    {
        marcherService.DeleteFromUndo(marcher, set, count, out _);
    }

    public static void HandleRawMoveOnConfirmed(MarcherPositionsManager marcher, int set, int count, PositionEntry after)
    {
        marcher.transform.position = after.pos;
        marcher.GetComponent<MarcherVisualStateController>()?.UpdateSelectorAnchor(set, count);
        director?.VisualizePathsForSet(set);
        director?.RefreshDashedPreviewForSelected();
    }

    public static void HandleRawMoveOnInferred(MarcherPositionsManager marcher, int set, int count, PositionEntry after)
    {
        marcher.transform.position = after.pos;
        marcher.GetComponent<MarcherVisualStateController>()?.SetSelectorVisible(false);
        director?.VisualizePathsForSet(set);
        director?.RefreshDashedPreviewForSelected();
    }

    public static void HandleRawMoveOnRaw(MarcherPositionsManager marcher, int set, int count, PositionEntry after)
    {
        marcher.transform.position = after.pos;
        marcher.GetComponent<MarcherVisualStateController>()?.SetSelectorVisible(true);
        director?.VisualizePathsForSet(set);
        director?.RefreshDashedPreviewForSelected();
    }
}

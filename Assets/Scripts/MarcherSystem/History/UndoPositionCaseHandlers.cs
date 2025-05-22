// This file defines per-case undo behavior handlers for each ChangeIntent in ApplyEntry().
// These should be invoked by switch(intent) in MarcherPositionHistory.ApplyEntry()

using UnityEngine;

public static class UndoPositionCaseHandlers
{
    private static MarcherPositionService marcherService;
    private static EnsembleDirector2 director;

    public static void Initialize(MarcherPositionService service, EnsembleDirector2 dir)
    {
        marcherService = service;
        director = dir;
    }

    public static void HandleConfirmedMoveOnConfirmed(MarcherPositionsManager marcher, int set, int count, PositionEntry before)
    {
        marcherService.DeleteFromUndo(marcher, set, count, out _);
        marcherService.ConfirmFromUndo(marcher, set, count, before.pos);
        marcher.transform.position = before.pos;

        marcher.GetComponent<MarcherVisualStateController>()?.SetSelectorVisible(true);
        director?.VisualizePathsForSet(set);
    }


    public static void HandleConfirmedMoveOnInferred(MarcherPositionsManager marcher, int set, int count, PositionEntry before)
    {
        marcherService.DeleteFromUndo(marcher, set, count, out _);
        marcherService.ConfirmFromUndo(marcher, set, count, before.pos);

        marcher.GetComponent<MarcherVisualStateController>()?.SetSelectorVisible(false);
        director?.VisualizePathsForSet(set);
    }


    public static void HandleConfirmedMoveOnRaw(MarcherPositionsManager marcher, int set, int count, PositionEntry before)
    {
        //marcherService.DeleteFromUndo(marcher, set, count, out _); //Only if confirmed position has no next confirmed position, maybe we handle this in an appropriate place?
        marcherService.ConfirmFromUndo(marcher, set, count, before.pos); //Else...

        marcher.GetComponent<MarcherVisualStateController>()?.SetSelectorVisible(false);
        marcher.GetComponent<MarcherVisualStateController>()?.UpdateSelectorAnchor(set, count);
        director?.VisualizePathsForSet(set);
        director?.RefreshDashedPreviewForSelected();
    }


    public static void HandleDeleteOnConfirmed(MarcherPositionsManager marcher, int set, int count, PositionEntry entry)
    {
        marcherService.ConfirmFromUndo(marcher, set, count, entry.pos);
        
        marcher.transform.position = entry.pos;
        marcher.GetComponent<MarcherVisualStateController>()?.SetSelectorVisible(true);
    }

    public static void HandleDeleteOnInferred(MarcherPositionsManager marcher, int set, int count, PositionEntry before)
    {
        marcherService.ConfirmFromUndo(marcher, set, count, before.pos);
        marcher.transform.position = before.pos;

        marcher.GetComponent<MarcherVisualStateController>()?.SetSelectorVisible(true);
        director?.VisualizePathsForSet(set);
    }


    public static void HandleRawMoveOnConfirmed(MarcherPositionsManager marcher, int set, int count, PositionEntry before)
    {
        marcher.transform.position = before.pos;

        marcher.GetComponent<MarcherVisualStateController>()?.UpdateSelectorAnchor(set, count);
        director?.VisualizePathsForSet(set);
        director?.RefreshDashedPreviewForSelected();
    }


    public static void HandleRawMoveOnInferred(MarcherPositionsManager marcher, int set, int count, PositionEntry before)
    {
        marcher.transform.position = before.pos;

        marcher.GetComponent<MarcherVisualStateController>()?.SetSelectorVisible(false);
        director?.VisualizePathsForSet(set);
        director?.RefreshDashedPreviewForSelected();
    }


    public static void HandleRawMoveOnRaw(MarcherPositionsManager marcher, int set, int count, PositionEntry before)
    {
        marcher.transform.position = before.pos;

        marcher.GetComponent<MarcherVisualStateController>()?.SetSelectorVisible(true);
        director?.VisualizePathsForSet(set);
        director?.RefreshDashedPreviewForSelected();
    }
}

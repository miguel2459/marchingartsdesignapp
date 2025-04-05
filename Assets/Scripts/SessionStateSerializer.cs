using UnityEngine;

public class SessionStateSerializer
{
    public static string ToJSON(UserStateSO user, ShowStateSO show, RuntimeCacheSO cache)
    {
        FullSessionState data = new FullSessionState
        {
            user = new UserStateData
            {
                UserId = user.UserId,
                UserEmail = user.UserEmail,
                UserName = user.UserName,
                AccountSheetID = user.AccountSheetID,
                UserFolderId = user.UserFolderId
            },
            show = new ShowStateData
            {
                CurrentShowID = show.CurrentShowID,
                ShowTitle = show.ShowTitle,
                GroupName = show.GroupName,
                CreatedBy = show.CreatedBy,
                FieldType = show.FieldType,
                ProductionYear = show.ProductionYear,
                NumberOfMarchers = show.NumberOfMarchers,
                NumberOfSets = show.NumberOfSets,
                NumberOfProps = show.NumberOfProps,
                LastModified = show.LastModified,
                ShowStatus = show.ShowStatus,
                LastSet = show.LastSet,
                JSONMarchersURL = show.JSONMarchersURL,
                JSONSetTimingURL = show.JSONSetTimingURL
            },
            cache = new RuntimeCacheData
            {
                CachedMarcherJSON = cache.CachedMarcherJSON,
                CachedTimingJSON = cache.CachedTimingJSON,
                MarchersCoordinates = cache.MarchersCoordinates,
                SetTimingMap = cache.SetTimingMap
            }
        };

        return JsonUtility.ToJson(data, true);
    }

    public static void LoadFromJSON(string json, UserStateSO user, ShowStateSO show, RuntimeCacheSO cache)
    {
        FullSessionState data = JsonUtility.FromJson<FullSessionState>(json);

        // USER
        user.UserId = data.user.UserId;
        user.UserEmail = data.user.UserEmail;
        user.UserName = data.user.UserName;
        user.AccountSheetID = data.user.AccountSheetID;
        user.UserFolderId = data.user.UserFolderId;

        // SHOW
        show.CurrentShowID = data.show.CurrentShowID;
        show.ShowTitle = data.show.ShowTitle;
        show.GroupName = data.show.GroupName;
        show.CreatedBy = data.show.CreatedBy;
        show.FieldType = data.show.FieldType;
        show.ProductionYear = data.show.ProductionYear;
        show.NumberOfMarchers = data.show.NumberOfMarchers;
        show.NumberOfSets = data.show.NumberOfSets;
        show.NumberOfProps = data.show.NumberOfProps;
        show.LastModified = data.show.LastModified;
        show.ShowStatus = data.show.ShowStatus;
        show.LastSet = data.show.LastSet;
        show.JSONMarchersURL = data.show.JSONMarchersURL;
        show.JSONSetTimingURL = data.show.JSONSetTimingURL;

        // RUNTIME CACHE
        cache.CachedMarcherJSON = data.cache.CachedMarcherJSON;
        cache.CachedTimingJSON = data.cache.CachedTimingJSON;
        cache.MarchersCoordinates = data.cache.MarchersCoordinates;
        cache.SetTimingMap = data.cache.SetTimingMap;
    }
}

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Services;
using Google.Apis.Sheets.v4;
using Google.Apis.Sheets.v4.Data;
using System.IO;
using System;

public class GoogleSheetsManager : MonoBehaviour
{
    private static string[] Scopes = { SheetsService.Scope.Spreadsheets };
    private static string ApplicationName = "MADA-Backend";
    private static string SpreadsheetId = "1LQuFCSrGrkWfqhmmeSj_3fttWnQhIYFMYrwlah13AeE"; // Replace with your Sheet ID
    private static string SheetName = "Main"; // Change if needed
    private SheetsService service;

    void Start()
    {
        AuthenticateGoogleAPI();
        AddNewUser("U12345", "Shawn", "shawn@email.com", "securepassXYZ");
    }

    void AuthenticateGoogleAPI()
    {
        string jsonPath = Path.Combine(Application.streamingAssetsPath, "service_account.json");

        GoogleCredential credential;
        using (var stream = new FileStream(jsonPath, FileMode.Open, FileAccess.Read))
        {
            credential = GoogleCredential.FromStream(stream)
                .CreateScoped(SheetsService.Scope.Spreadsheets);
        }

        service = new SheetsService(new BaseClientService.Initializer()
        {
            HttpClientInitializer = credential,
            ApplicationName = ApplicationName,
        });

        Debug.Log("✅ Google Sheets API Authentication Successful with Service Account!");
    }


    public void AddNewUser(string userId, string name, string email, string password)
    {
        List<IList<object>> values = new List<IList<object>>
        {
            new List<object> { userId, name, email, password, DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss") }
        };

        ValueRange body = new ValueRange()
        {
            Values = values
        };

        SpreadsheetsResource.ValuesResource.AppendRequest request = service.Spreadsheets.Values.Append(body, SpreadsheetId, SheetName + "!A:E");
        request.ValueInputOption = SpreadsheetsResource.ValuesResource.AppendRequest.ValueInputOptionEnum.USERENTERED;

        var response = request.Execute();
        Debug.Log("✅ New User Added to Google Sheets!");
    }
}

//mada-sheets-bot@mada-backend.iam.gserviceaccount.com
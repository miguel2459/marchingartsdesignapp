using System.Collections;
using UnityEngine;

public class ShowSceneInitializer : MonoBehaviour
{
    [Header("Scene Dependencies")]
    public SessionBootstrapper sessionBootstrapper;
    public EnsembleSessionLoader ensembleLoader;
    public EnsembleDirector2 director;
    public MarcherManager marcherManager;
    public SelectedMarchers selectedMarchers;
    public SetProgressBar setProgressBar;
    public EnsembleUIController uiController;
    public CameraModeManager cameraModeManager;
    public UserAccountManager userAccountManager;

    private bool initialized = false;

    private void Start()
    {
        StartCoroutine(InitializeSceneFlow());
    }

    private IEnumerator InitializeSceneFlow()
    {
        Debug.Log("🔧 [Initializer] Starting ShowScene initialization...");

        // 1. Init Bootstrapper and SessionLoader
        Debug.Log("⚙️ [Step 1] Init SessionBootstrapper and SessionLoader...");
        sessionBootstrapper.SessionBootstrapperInit();
        ensembleLoader.EnsembleLoaderInit();

        // 2. Init EnsembleDirector & MarcherManager
        Debug.Log("⚙️ [Step 2] Init FieldGrid & MarcherManager...");
        director.fieldManager.FieldGridInit();
        marcherManager.MarcherManagerInit();

        // 🔑 Subscribe BEFORE starting bootstrapper
        Debug.Log("⚙️ [Step 3] Hook OnMarchersReady early...");
        bool marchersReady = false;
        marcherManager.OnMarchersReady += () => marchersReady = true;

        // 3. Start SessionBootstrapper Coroutine
        Debug.Log("🚀 [Step 4] Start BootStrapperStart coroutine...");
        yield return StartCoroutine(sessionBootstrapper.BootStrapperStart());

        // 4. Wait for Marchers
        Debug.Log("⏳ [Step 5] Waiting for MarcherManager to build marchers...");
        yield return new WaitUntil(() => marchersReady);
        Debug.Log("✅ Marchers built and positioned.");

        // 5–8
        Debug.Log("⚙️ [Step 6] Init SelectedMarchers...");
        selectedMarchers.SelectedMarchersInit();

        Debug.Log("⚙️ [Step 7] Init SetProgressBar...");
        setProgressBar.SetProgressBarInit();

        // Debug.Log("⚙️ [Step 8] Init EnsembleUIController...");
        uiController.InitializeUI();
        userAccountManager.UserAccountInit();

        Debug.Log("⚙️ [Step 9] Init Camera ModeManager...");
        cameraModeManager.CameraModeInit();

        initialized = true;
        Debug.Log("✅✅✅ [Initializer] ShowScene fully initialized.");
    }
}

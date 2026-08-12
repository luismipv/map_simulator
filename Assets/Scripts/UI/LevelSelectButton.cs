using UnityEngine;

public class LevelSelectButton : MonoBehaviour
{
    [Header("El nivel que cargará este botón")]
    public LevelData levelToLoad;
    private LevelState levelState;

    // Esta es la función que conectarás en el evento OnClick del botón en el Inspector

    void Awake() {
        levelState = LevelState.Instance;
        OnRecoverLevelFromState();
    }

    private void OnRecoverLevelFromState(){
        if(levelState?.SelectedLevelData){
            this.levelToLoad = levelState.SelectedLevelData;
        }
    }

    public void GoToLevel()
    {
        if (Logic.Instance != null)
        {
            Logic.Instance.LoadSpecificLevel(levelToLoad);
        }
        else
        {
            Debug.LogError("No se encontró el Logic Manager en la escena.");
        }
    }
}
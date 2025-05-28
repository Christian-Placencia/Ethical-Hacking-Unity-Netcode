using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using System.Linq;

public class DeliveryManager : NetworkBehaviour {


    public event EventHandler OnRecipeSpawned;
    public event EventHandler OnRecipeCompleted;
    public event EventHandler OnRecipeSuccess;
    public event EventHandler OnRecipeFailed;


    public static DeliveryManager Instance { get; private set; }


    [SerializeField] private RecipeListSO recipeListSO;


    private List<RecipeSO> waitingRecipeSOList;
    private float spawnRecipeTimer = 4f;
    private float spawnRecipeTimerMax = 4f;
    private int waitingRecipesMax = 4;
    private int successfulRecipesAmount;


    private void Awake() {
        Instance = this;


        waitingRecipeSOList = new List<RecipeSO>();
    }

    private void Update() {
        if (!IsServer) {
            return; 
        }
        spawnRecipeTimer -= Time.deltaTime;
        if (spawnRecipeTimer <= 0f) {
            spawnRecipeTimer = spawnRecipeTimerMax;

            if (KitchenGameManager.Instance.IsGamePlaying() && waitingRecipeSOList.Count < waitingRecipesMax) {
                int waitingRecipeSOIndex = UnityEngine.Random.Range(0, recipeListSO.recipeSOList.Count);
                SpawnNewWaitingRecipeClientRpc(waitingRecipeSOIndex);
            }
        }
    }

    // Only the server can call this method, but it will be called on all clients.
    [ClientRpc]
    private void SpawnNewWaitingRecipeClientRpc(int waitingRecipeSOIndex) 
    {
        RecipeSO waitingRecipeSO = recipeListSO.recipeSOList[waitingRecipeSOIndex];
        waitingRecipeSOList.Add(waitingRecipeSO);
        // Notify all clients that a new recipe has been spawned.
        OnRecipeSpawned?.Invoke(this, EventArgs.Empty);
    }


    // Change this method to call a ServerRpc instead of directly validating
    public void DeliverRecipe(PlateKitchenObject plateKitchenObject) {
        // Send plate contents to server for validation
        List<int> plateKitchenObjectSOIds = new List<int>();
        foreach (KitchenObjectSO kitchenObjectSO in plateKitchenObject.GetKitchenObjectSOList()) {
            // We'll send IDs rather than whole objects over network
            plateKitchenObjectSOIds.Add(kitchenObjectSO.objectId);
        }
        
        // Call server to validate the recipe
        ValidateRecipeServerRpc(plateKitchenObjectSOIds.ToArray());
    }

    // New method to handle server-side validation 
    [ServerRpc(RequireOwnership = false)]
    private void ValidateRecipeServerRpc(int[] plateKitchenObjectSOIds) {
        // Convert back to KitchenObjectSO list on server
        List<KitchenObjectSO> plateContents = new List<KitchenObjectSO>();
        foreach (int objectId in plateKitchenObjectSOIds) {
            KitchenObjectSO kitchenObjectSO = GetKitchenObjectSOById(objectId);
            if (kitchenObjectSO != null) {
                plateContents.Add(kitchenObjectSO);
            }
        }
        
        // Server-side validation
        for (int i = 0; i < waitingRecipeSOList.Count; i++) {
            RecipeSO waitingRecipeSO = waitingRecipeSOList[i];

            if (waitingRecipeSO.kitchenObjectSOList.Count == plateContents.Count) {
                // Has the same number of ingredients
                bool plateContentsMatchesRecipe = true;
                foreach (KitchenObjectSO recipeKitchenObjectSO in waitingRecipeSO.kitchenObjectSOList) {
                    // Cycling through all ingredients in the Recipe
                    bool ingredientFound = false;
                    foreach (KitchenObjectSO plateKitchenObjectSO in plateContents) {
                        // Cycling through all ingredients in the Plate
                        if (plateKitchenObjectSO == recipeKitchenObjectSO) {
                            // Ingredient matches!
                            ingredientFound = true;
                            break;
                        }
                    }
                    if (!ingredientFound) {
                        // This Recipe ingredient was not found on the Plate
                        plateContentsMatchesRecipe = false;
                    }
                }

                if (plateContentsMatchesRecipe) {
                    // Player delivered the correct recipe!
                    DeliverCorrectRecipeClientRpc(i);
                    return;
                }
            }
        }

        // No matches found!
        // Player did not deliver a correct recipe
        DeliverIncorrecRecipeClientRpc();
    }

    // Helper method to get KitchenObjectSO by ID
    private KitchenObjectSO GetKitchenObjectSOById(int objectId) {
        // Lookup in your KitchenObjectSO database
        // You'll need to ensure each KitchenObjectSO has a unique ID
        foreach (KitchenObjectSO kitchenObjectSO in recipeListSO.GetAllKitchenObjectSOs()) {
            if (kitchenObjectSO.objectId == objectId) {
                return kitchenObjectSO;
            }
        }
        return null;
    }

    // Incorrect deliveries
    [ClientRpc]
    private void DeliverIncorrecRecipeClientRpc()
    {
        OnRecipeFailed?.Invoke(this, EventArgs.Empty);
    }

    // Correct delivieries
    [ClientRpc]
    private void DeliverCorrectRecipeClientRpc(int waitingRecipeSOListIndex)
    {
        successfulRecipesAmount++;

        waitingRecipeSOList.RemoveAt(waitingRecipeSOListIndex);

        OnRecipeCompleted?.Invoke(this, EventArgs.Empty);
        OnRecipeSuccess?.Invoke(this, EventArgs.Empty);
    }

    public List<RecipeSO> GetWaitingRecipeSOList() {
        return waitingRecipeSOList;
    }

    public int GetSuccessfulRecipesAmount() {
        return successfulRecipesAmount;
    }

}

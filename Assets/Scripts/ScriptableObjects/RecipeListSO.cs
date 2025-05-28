using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

//[CreateAssetMenu()]
public class RecipeListSO : ScriptableObject {

    public List<RecipeSO> recipeSOList;

    // Add this method
    public List<KitchenObjectSO> GetAllKitchenObjectSOs() {
        // Return a list of all unique KitchenObjectSO used across all recipes
        HashSet<KitchenObjectSO> uniqueObjects = new HashSet<KitchenObjectSO>();
        
        foreach (RecipeSO recipe in recipeSOList) {
            foreach (KitchenObjectSO kitchenObject in recipe.kitchenObjectSOList) {
                uniqueObjects.Add(kitchenObject);
            }
        }
        
        return uniqueObjects.ToList();
    }
}
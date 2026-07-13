using UnityEngine;

[CreateAssetMenu(fileName = "RecipePool", menuName = "DungeonFront/Recipe Pool")]
public class RecipePool : ScriptableObject
{
    public Recipe[] recipes;

    public bool Contains(Recipe recipe)
    {
        if (recipe == null || recipes == null)
        {
            return false;
        }

        foreach (Recipe entry in recipes)
        {
            if (entry == recipe)
            {
                return true;
            }
        }

        return false;
    }

    public Recipe GetFirst()
    {
        if (recipes == null || recipes.Length == 0)
        {
            return null;
        }

        return recipes[0];
    }
}

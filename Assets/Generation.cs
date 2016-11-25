using UnityEngine;
using System.Collections;

public class Generation : MonoBehaviour
{

	#region Editor;
    
    public int width = 200;
    public int height = 200;
    public string seed; // seed
    public bool randomSeed; // use of a random seed?
    public bool fastGen; // false = manual steps
    [Range(0, 100)]
    public int waterPercent = 45; // water/land percentage
    [Range(0, 100)]
    public int waterBorder = 25; // limits the continent on the sides
	
	#endregion Editor;

    private int[,] map; // matrice representing the world (0: water, 1: temperate, 2: cold, 3: warm)
    private bool land = false; // is land generated?
    private bool biomes = false; // are biomes generated?
    private bool smooth = false; // refining done?
    
    void Update()
    {
        if (fastGen && Input.GetMouseButtonUp(0)) Begin(); // fast generation (steps)
        else
        {
            // manual generation (steps)
            if (Input.GetMouseButtonUp(0)) Generate();
            if (Input.GetMouseButtonUp(1)) Smooth();
        }
    }

    private void Begin()
    {
        if (fastGen)
        {
            Generate(); // generate land
            Smooth(); // 1st land refining
            Smooth(); // 2nd land refining
            Generate(); // generate biomes
            Smooth(); // biomes refining
			// generation of a basic continent
        }
    }

    #region Generic;

    // regarding which step we are in,
    // we create land or biomes
    private void Generate()
    {
        if (land) GenerateBiomes();
        else GenerateLand();
    }

    // regarding which step we are in,
    // we refine land or biomes
    private void Smooth()
    {
        if (biomes) SmoothBiomes();
        else SmoothLand();
    }

    // visualization
    void OnDrawGizmos()
    {
        // we place a colored square for each
        // matrice cell regarding the biome
        // 1 : water => blue
        // 2 : temperate => green
        // 3 : cold => white
        // 4 : warm => yellow
        if (map != null)
        {
            for (int i = 0; i < width; i++)
            {
                for (int j = 0; j < height; j++)
                {
                    switch (map[i, j])
                    {
                        case 0: Gizmos.color = Color.blue; break;
                        case 1: Gizmos.color = Color.green; break;
                        case 2: Gizmos.color = Color.white; break;
                        case 3: Gizmos.color = Color.yellow; break;
                    }
                    Vector2 pos = new Vector2(-width / 2 + i + .5f, -height / 2 + j + .5f);
                    Gizmos.DrawCube(pos, Vector2.one);
                }
            }
        }
    }

    #endregion Generic;

    #region LandGeneration;

	// land generation
    private void GenerateLand()
    {
        map = new int[width, height];
        land = true;
        biomes = false;
        smooth = false;
        if (randomSeed) seed = Time.time.ToString(); // if randomSeed, we generate a seed from current time
        System.Random rand = new System.Random(seed.GetHashCode());
		
		// we start with a spray water (0) / land (1)
        for (int i = 0; i < width; i++)
        {
            for (int j = 0; j < height; j++)
            {
				// farther we are from the water bounds, bigger are the chances to generate land
                float farDiff = 
                    (waterBorder * Mathf.Abs((width / 2.0f - i) / (width / 2.0f))) +
                    (waterBorder * Mathf.Abs((height / 2.0f - j) / (height / 2.0f)));
                map[i, j] = (rand.Next(0, 100) < waterPercent + farDiff) ? 0 : 1;
            }
        }
    }

    private void SmoothLand()
    {
        for (int i = 0; i < width; i++)
        {
            for (int j = 0; j < height; j++)
            {
				// for each cell, we observe surrounding cells and we count
				// those corresponding to land (CountLand)
				// if this is the first refining (smooth == false), we only look for adjacent cells
				// otherwise, we also look for cells adjacent to the neighbooring cells
				// this allow to have a first refining that will create a big mass (continent)
				// then the next refines will allow to smooth the continent
                int count = CountLand(i, j);
                if (count < (smooth ? 4 : 9)) map[i, j] = 0;
                else if (count > (smooth ? 4 : 12)) map[i, j] = 1;
            }
        }
        smooth = true; // for the next refines
    }

    private int CountLand(int x, int y)
    {
        int res = 0;
        for (int i = x - (smooth ? 1 : 2); i <= x + (smooth ? 1 : 2); i++)
        {
            for (int j = y - (smooth ? 1 : 2); j <= y + (smooth ? 1 : 2); j++)
            {
                if (i >= 0 && j >= 0 && i < width && j < height) res += map[i, j];
            }
        }
        return res - map[x, y];
    }

    #endregion LandGeneration;

    #region BiomesGeneration;

	// same for biomes generation
    // but il will only be on
    // already generated land (1)
	
    private void GenerateBiomes()
    {
        land = false;
        biomes = true;
        smooth = false;
        if (randomSeed) seed = Time.time.ToString();
        System.Random rand = new System.Random(seed.GetHashCode());
        for (int i = 0; i < width; i++)
        {
            for (int j = 0; j < height; j++)
            {
                if (map[i, j] == 1)
                {
                    if (j > height / 2 && rand.Next(0, 100) < 100.0f * j / height) map[i, j] = 2;
                    else if (j < height / 2 && rand.Next(0, 100) < 100.0f * (height - j) / height) map[i, j] = 3;
                }
            }
        }
    }

    private void SmoothBiomes()
    {
        for (int i = 0; i < width; i++)
        {
            for (int j = 0; j < height; j++)
            {
                if (map[i, j] != 0)
                {
                    int[] counts = CountBiomes(i, j);
                    if (counts[2] > counts[1] && counts[2] > counts[3]) map[i, j] = 2;
                    else if (counts[3] > counts[1] && counts[3] > counts[2]) map[i, j] = 3;
                    else map[i, j] = 1;
                }
            }
        }
    }

    private int[] CountBiomes(int x, int y)
    {
        int[] res = new int[4];
        for (int i = x - (smooth ? 1 : 2); i <= x + (smooth ? 1 : 2); i++)
        {
            for (int j = y - (smooth ? 1 : 2); j <= y + (smooth ? 1 : 2); j++)
            {
                if (i >= 0 && j >= 0 && i < width && j < height && (i != x || j != y)) res[map[i, j]]++;
            }
        }
        return res;
    }

    #endregion BiomesGeneration;

}

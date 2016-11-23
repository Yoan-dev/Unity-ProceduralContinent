using UnityEngine;
using System.Collections;

public class Generation : MonoBehaviour
{
	
	/*
	* made by Yoan Bocquelet
	* C#/Unity
	* Génération procédurale d'une forme continentale avec séparation en biomes
	* Utilisation du principe d'automate cellulaire à partir duquel j'ai créé mon propre algorithme
	*/

	#region Editor;

	// Ces variables sont public pour
	// être éditées directement au sein
	// de Unity (debug / tests)
    public int width = 200; // largeur du monde
    public int height = 200; // hauteur du monde
    public string seed; // seed à utiliser
    public bool randomSeed; // est-ce qu'on utilise une seed aléatoire
    public bool fastGen; // génération automatique (true) / manuelle (false)
    [Range(0, 100)]
    public int waterPercent = 45; // pourcentage d'eau dans notre monde
    [Range(0, 100)]
    public int waterBorder = 25; // largeur des bandes d'eau sur les côtés du monde (effet île)
	
	#endregion Editor;

    private int[,] map; // la matrice représentant notre monde
    private bool land = false; // si on a déjà généré la terre
    private bool biomes = false; // si on a déjà généré les biomes
    private bool smooth = false; // si on a déjà effectué un affinage

	// fonction généré par Unity et appelé à l'initialisation
    void Start()
    {
        Begin();
    }

    void Begin()
    {
        if (fastGen) // si on est en génération rapide
        {
            Generate(); // on génère la terre
            Smooth(); // 1er affinage
            Smooth(); // 2ème affinage
            Generate(); // on génère les biomes
            Smooth(); // affinage des biomes
			// cela donne un continent "standard"
        }
    }

	// fonction généré par Unity et appelé à chaque update du moteur
    void Update()
    {
        if (fastGen && Input.GetMouseButtonDown(0)) Begin(); // on peut relancer une génération rapide
        else
        {
			// ou on peut soit même décider du nombre d'affinage, etc.
            if (Input.GetMouseButtonDown(0)) Generate();
            if (Input.GetMouseButtonDown(1)) Smooth();
        }
    }

    #region Generic;

	// suivant l'étape à laquelle on est rendu,
	// on affine les biomes ou la terre
    private void Smooth()
    {
        if (biomes) SmoothBiomes();
        else SmoothLand();
    }

	// suivant l'étape à laquelle on est rendu,
	// on créé les biomes ou la terre
    private void Generate()
    {
        if (land) GenerateBiomes();
        else GenerateLand();
    }

	// fonction Unity pour avoir une représentation
	// visuelle de notre continent (ne marche que dans l'éditeur)
	// (projet expérimental qui n'a pas vocation à être build en .exe pour le moment)
    void OnDrawGizmos()
    {
		// on itère sur notre matrice et on place
		// un carré de couleur en fonction de la case
		// 1 : eau => bleue
		// 2 : biome tempéré => vert
		// 3 : biome froid => blanc
		// 4 : biome chaud => jaune
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

	// génération de la terre
    private void GenerateLand()
    {
        map = new int[width, height];
        land = true;
        biomes = false;
        smooth = false;
        if (randomSeed) seed = Time.time.ToString(); // si seed aléatoire, on la génère en fonction du temps
        System.Random rand = new System.Random(seed.GetHashCode()); // création d'un objet Random
		
		// on commence par faire un spray de cases eau (0) / terre (1)
        for (int i = 0; i < width; i++)
        {
            for (int j = 0; j < height; j++)
            {
				// plus on est loin des bordures, plus on a de chance d'avoir une case de terre
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
				// pour chaque case, on observe les cases alentours et on compte
				// celles qui correspondent à de la terre (CountLand)
				// si c'est le premier affinage (smooth = false), on observe seulement les cases voisines (cf CountLand)
				// sinon, on observe également les cases voisines aux voisines
				// ceci a pour effet d'avoir un premier affinage qui va créer de gros blocs (effet continent)
				// alors que les affinages suivant vont permettre de lisser ce continent
                int count = CountLand(i, j);
                if (count < (smooth ? 4 : 9)) map[i, j] = 0;
                else if (count > (smooth ? 4 : 12)) map[i, j] = 1;
            }
        }
        smooth = true; // on mets smooth à true pour les affinages suivant
    }

    private int CountLand(int x, int y)
    {
		// on renvoi le nombre de case de terre alentours
		// en fonction de smooth (seulement les voisines ou également leurs voisines)
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

	// même principe pour les biomes
	// sauf que ceux ci ne seront générés
	// que sur les cases de terre (1)
	
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

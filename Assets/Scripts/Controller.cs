using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

public class Controller : MonoBehaviour
{
    //GameObjects
    public GameObject board;
    public GameObject[] cops = new GameObject[2];
    public GameObject robber;
    public Text rounds;
    public Text finalMessage;
    public Button playAgainButton;

    //Otras variables
    Tile[] tiles = new Tile[Constants.NumTiles];
    private int roundCount = 0;
    private int state;
    private int clickedTile = -1;
    private int clickedCop = 0;
    Dictionary<Tile, List<int>> robberDistance = new Dictionary<Tile, List<int>>();


    void Start()
    {        
        InitTiles();
        InitAdjacencyLists();
        state = Constants.Init;
    }
        
    //Rellenamos el array de casillas y posicionamos las fichas
    void InitTiles()
    {
        for (int fil = 0; fil < Constants.TilesPerRow; fil++)
        {
            GameObject rowchild = board.transform.GetChild(fil).gameObject;            

            for (int col = 0; col < Constants.TilesPerRow; col++)
            {
                GameObject tilechild = rowchild.transform.GetChild(col).gameObject;                
                tiles[fil * Constants.TilesPerRow + col] = tilechild.GetComponent<Tile>();                         
            }
        }
                
        cops[0].GetComponent<CopMove>().currentTile=Constants.InitialCop0;
        cops[1].GetComponent<CopMove>().currentTile=Constants.InitialCop1;
        robber.GetComponent<RobberMove>().currentTile=Constants.InitialRobber;           
    }

    public void InitAdjacencyLists()
    {
        //Matriz de adyacencia
        int[,] matriu = new int[Constants.NumTiles, Constants.NumTiles];

        //TODO: Inicializar matriz a 0's
        for (int i = 0; i < Constants.NumTiles; ++i)
        {
        for (int j = 0; j < Constants.NumTiles; ++j)
        {
            matriu[i, j] = 0;
        }
        }

        //TODO: Para cada posición, rellenar con 1's las casillas adyacentes (arriba, abajo, izquierda y derecha)
        for (int i = 0; i < Constants.NumTiles; ++i)
        {
            // Arriba
            if (i > 7) { 
                matriu[i, i - 8] = 1; 
            }

            // Abajo
            if (i < 56) {
                matriu[i, i + 8] = 1; 
            }

            // Derecha
            if ( ( (i + 1) % 8) != 0) { 
                matriu[i, i + 1] = 1; 
            }

            // Izquierda
            if (i % 8 != 0) {
                matriu[i, i - 1] = 1; 
            }
        }

        //TODO: Rellenar la lista "adjacency" de cada casilla con los índices de sus casillas adyacentes
        for (int i = 0; i < Constants.NumTiles; ++i)
        {
            for (int j = 0; j < Constants.NumTiles; ++j)
            {
                if (matriu[i, j] == 1)
                {
                    tiles[i].adjacency.Add(j);
                }
            }
        }

    }

    //Reseteamos cada casilla: color, padre, distancia y visitada
    public void ResetTiles()
    {        
        foreach (Tile tile in tiles)
        {
            tile.Reset();
        }
    }

    public void ClickOnCop(int cop_id)
    {
        switch (state)
        {
            case Constants.Init:
            case Constants.CopSelected:                
                clickedCop = cop_id;
                clickedTile = cops[cop_id].GetComponent<CopMove>().currentTile;
                tiles[clickedTile].current = true;

                ResetTiles();
                FindSelectableTiles(true);

                state = Constants.CopSelected;                
                break;            
        }
    }

    public void ClickOnTile(int t)
    {                     
        clickedTile = t;

        switch (state)
        {            
            case Constants.CopSelected:
                //Si es una casilla roja, nos movemos
                if (tiles[clickedTile].selectable)
                {                  
                    cops[clickedCop].GetComponent<CopMove>().MoveToTile(tiles[clickedTile]);
                    cops[clickedCop].GetComponent<CopMove>().currentTile=tiles[clickedTile].numTile;
                    tiles[clickedTile].current = true;   
                    
                    state = Constants.TileSelected;
                }                
                break;
            case Constants.TileSelected:
                state = Constants.Init;
                break;
            case Constants.RobberTurn:
                state = Constants.Init;
                break;
        }
    }

    public void FinishTurn()
    {
        switch (state)
        {            
            case Constants.TileSelected:
                ResetTiles();

                state = Constants.RobberTurn;
                RobberTurn();
                break;
            case Constants.RobberTurn:                
                ResetTiles();
                IncreaseRoundCount();
                if (roundCount <= Constants.MaxRounds)
                    state = Constants.Init;
                else
                    EndGame(false);
                break;
        }

    }

    public void RobberTurn()
    {
        RobberMove robberMove = robber.GetComponent<RobberMove>();
        clickedTile = robberMove.currentTile;
        tiles[clickedTile].current = true;
        FindSelectableTiles(false);


        //Agregar casillas seleccionables al diccionario
        robberDistance.Clear();
        foreach (Tile t in tiles)
        {
            if (t.selectable)
            {
                robberDistance.Add(t, new List<int>());
            }
        }

        for (int i = 0; i < cops.Length; ++i)
        {
            clickedCop = i;
            clickedTile = cops[i].GetComponent<CopMove>().currentTile;
            tiles[clickedTile].current = true;

            // Update after each cop
            ResetTiles();
            FindSelectableTiles(true);
        }

        int maxDistance = 0;
        Tile finalTile = new Tile();

        foreach (Tile t in robberDistance.Keys)
        {
            // Pick the one that's the furthest from them all
            if (robberDistance[t].Sum() > maxDistance)
            {
                finalTile = t;
                maxDistance = robberDistance[t].Sum();
            }

            // Otherwise, pick the one with the largest distance numbers
            else if (robberDistance[t].Sum() == maxDistance)
            {
                bool isFurther = true;
                foreach (int d in robberDistance[t])
                {
                    if (d < robberDistance[finalTile][0]
                    && d < robberDistance[finalTile][1])
                    {
                        isFurther = false;
                    }
                }

                if (isFurther) { finalTile = t; }
            }
        }

        ResetTiles();
        robberMove.currentTile = finalTile.numTile;
        robberMove.MoveToTile(finalTile);
        /*TODO: Cambia el código de abajo para hacer lo siguiente
        - Elegimos una casilla aleatoria entre las seleccionables que puede ir el caco
        - Movemos al caco a esa casilla
        - Actualizamos la variable currentTile del caco a la nueva casilla
        */
        //robber.GetComponent<RobberMove>().MoveToTile(tiles[robber.GetComponent<RobberMove>().currentTile]);

    }

    public void EndGame(bool end)
    {
        if(end)
            finalMessage.text = "You Win!";
        else
            finalMessage.text = "You Lose!";
        playAgainButton.interactable = true;
        state = Constants.End;
    }

    public void PlayAgain()
    {
        cops[0].GetComponent<CopMove>().Restart(tiles[Constants.InitialCop0]);
        cops[1].GetComponent<CopMove>().Restart(tiles[Constants.InitialCop1]);
        robber.GetComponent<RobberMove>().Restart(tiles[Constants.InitialRobber]);
                
        ResetTiles();

        playAgainButton.interactable = false;
        finalMessage.text = "";
        roundCount = 0;
        rounds.text = "Rounds: ";

        state = Constants.Restarting;
    }

    public void InitGame()
    {
        state = Constants.Init;
         
    }

    public void IncreaseRoundCount()
    {
        roundCount++;
        rounds.text = "Rounds: " + roundCount;
    }

    public void FindSelectableTiles(bool cop)
    {
                 
        int indexcurrentTile;        

        if (cop==true)
            indexcurrentTile = cops[clickedCop].GetComponent<CopMove>().currentTile;
        else
            indexcurrentTile = robber.GetComponent<RobberMove>().currentTile;

        //La ponemos rosa porque acabamos de hacer un reset
        tiles[indexcurrentTile].current = true;
        tiles[indexcurrentTile].visited = true;


        //Lista posiciones demas policias
        List<int> indices = new List<int>();
        foreach (GameObject c in cops)
        {
            indices.Add(c.GetComponent<CopMove>().currentTile);
        }


        //TODO: Implementar BFS. Los nodos seleccionables los ponemos como selectable=false
        foreach (Tile t in tiles)
        {
            t.selectable = false;
        }


        //Cola para el BFS
        Queue<Tile> nodes = new Queue<Tile>();

        foreach (Tile t in tiles)
        {
            t.selectable = false;
        };

        foreach (int i in tiles[indexcurrentTile].adjacency)
        {
            tiles[i].parent = tiles[indexcurrentTile];
            nodes.Enqueue(tiles[i]);
        }

        while (nodes.Count > 0)
        {
            Tile curr = nodes.Dequeue();
            if (!curr.visited)
            {
                if (indices.Contains(curr.numTile))
                {
                    curr.distance = curr.parent.distance + 1;
                    curr.visited = true;
                }
                else
                {
                    foreach (int i in curr.adjacency)
                    {
                        if (!tiles[i].visited)
                        {
                            tiles[i].parent = curr;
                            nodes.Enqueue(tiles[i]);
                        }
                    }

                    curr.visited = true;
                    curr.distance = curr.parent.distance + 1;
                }
            }
        }

        //TODO: Implementar BFS. Los nodos seleccionables los ponemos como selectable=true
        //Tendrás que cambiar este código por el BFS
        foreach (Tile t in tiles)
        {
            if (!indices.Contains(t.numTile)
            && t.distance <= Constants.Distance && t.distance > 0)
            {
                t.selectable = true;
            }
        }


    }
    
   
    

    

   

       
}

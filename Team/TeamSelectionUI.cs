// using UnityEngine;
// using Mirror;

// public class TeamSelection : MonoBehaviour
// {
//     public GameObject characterSelection; 
//     public static int chosenTeam = -1; 

//     void Start()
//     {
//         gameObject.SetActive(true);
//         characterSelection.SetActive(false);
//     }

//     public void SelectTeam(int teamID)
//     {
//         chosenTeam = teamID;
//         gameObject.SetActive(false);
//         characterSelection.SetActive(true);
//     }
// }

























// // using UnityEngine;
// // using Mirror;
// // public class TeamSelection : MonoBehaviour
// // {
// //     [Header("UI Elements")]
// //     public GameObject characterSelection; // Reference to Character Selection GameObject (Separate)

// //     public static int chosenTeam = -1; // -1 = No team selected, 0 = Blue, 1 = Red
    
    
// //     void Start()
// //     {
        
// //         gameObject.SetActive(true); // Show team selection UI, disable character selection
// //         characterSelection.SetActive(false);
       


// //     }

// //     public void SelectTeam(int teamID)
// //     {
// //         chosenTeam = teamID; // Store chosen team
// //          gameObject.SetActive(false);

// //         // Activate Character Selection UI
// //         characterSelection.SetActive(true);
// //         // Hide team selection UI
// //         // if (chosenTeam == 0)
// //         // {
// //         //     GameManager.instance.BluePlayersCount++;
// //         // }
// //         // else if (chosenTeam == 1)
// //         // {
// //         //     GameManager.instance.RedPlayersCount++;
// //         // }

// //         // if (GameManager.instance.BluePlayersCount > GameManager.instance.MaxBluePlayers)
// //         // {
// //         //     Debug.LogError("BLUE Team" + TeamSelection.chosenTeam + "Is Full");
// //         //     GameManager.instance.BluePlayersCount = GameManager.instance.MaxBluePlayers;
// //         //     SelectTeam(1);
// //         //     gameObject.SetActive(false);

// //         //     // Activate Character Selection UI
// //         //     characterSelection.SetActive(true);

// //         // }
// //         // else if (GameManager.instance.BluePlayersCount > GameManager.instance.MaxBluePlayers)
// //         // {
// //         //     Debug.LogError("RED Team" + TeamSelection.chosenTeam + "Is Full");
// //         //     GameManager.instance.RedPlayersCount = GameManager.instance.MaxRedPlayers;
// //         //     SelectTeam(0);
// //         //     gameObject.SetActive(false);

// //         //     // Activate Character Selection UI
// //         //     characterSelection.SetActive(true);
// //         // }
       

// //     }

// // }
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TDMManager : NetworkObject
{
    public const ushort TEAM_BLUE = 1, TEAM_RED = 2;
    [SerializeField] Transform blueStart, redStart;
    [SerializeField] List<PositionTracker> teamBlue = new List<PositionTracker>();
    [SerializeField] List<PositionTracker> teamRed = new List<PositionTracker>();

    [SerializeField] GameObject doppel;
    [SerializeField] Doppel[] blueDoppels;
    [SerializeField] Doppel[] redDoppels;
    // Start is called before the first frame update
    new void Start()
    {
        base.Start();
        NetworkManager.networkObjects.Add(1, GetComponent<TDMManager>());

        blueDoppels = new Doppel[2];
        redDoppels = new Doppel[2];

        buffer3 = new byte[3];
        NetworkManager.SetBufferUShort(buffer3, 1);
    }

    void StartMatch()
    {
        NetworkManager.players[64].gamemode = PlayerController.GM_TDM;
        NetworkManager.players[65].gamemode = PlayerController.GM_TDM;

        CreateDoppels();
        SetTeams();
    }

    void CreateDoppels()
    {
        blueDoppels[0] = Instantiate(doppel, Vector3.zero, Quaternion.identity).GetComponent<Doppel>();
        blueDoppels[1] = Instantiate(doppel, Vector3.zero, Quaternion.identity).GetComponent<Doppel>();
        teamBlue.Add(blueDoppels[0].posTracker);
        teamBlue.Add(blueDoppels[1].posTracker);
        Debug.Log(teamBlue);

        blueDoppels[0].AssignObjID(1000);
        blueDoppels[1].AssignObjID(1001);

        redDoppels[0] = Instantiate(doppel, Vector3.zero, Quaternion.identity).GetComponent<Doppel>();
        redDoppels[1] = Instantiate(doppel, Vector3.zero, Quaternion.identity).GetComponent<Doppel>();        
        teamRed.Add(redDoppels[0].posTracker);
        teamRed.Add(redDoppels[1].posTracker);

        redDoppels[0].AssignObjID(1002);
        redDoppels[1].AssignObjID(1003);

        blueDoppels[0].trfm.position = blueStart.position + Vector3.right * 5;
        blueDoppels[1].trfm.position = blueStart.position + Vector3.right * -5;

        redDoppels[0].trfm.position = redStart.position + Vector3.right * 5;
        redDoppels[1].trfm.position = redStart.position + Vector3.right * -5;
    }

    PlayerController localPlayer, opposingPlayer;
    void SetTeams()
    {
        NetworkManager.players[64].AssignTeam(TEAM_BLUE);
        NetworkManager.players[65].AssignTeam(TEAM_RED);

        teamBlue.Add(NetworkManager.players[64].posTracker);
        teamRed.Add(NetworkManager.players[65].posTracker);

        blueDoppels[0].Init(TEAM_BLUE, NetworkManager.players[64], teamRed, PlayerController.playerObjID == 64);
        blueDoppels[1].Init(TEAM_BLUE, NetworkManager.players[64], teamRed, PlayerController.playerObjID == 64);

        redDoppels[0].Init(TEAM_RED, NetworkManager.players[65], teamBlue, PlayerController.playerObjID == 65);
        redDoppels[1].Init(TEAM_RED, NetworkManager.players[65], teamBlue, PlayerController.playerObjID == 65);

        NetworkManager.players[64].trfm.position = blueStart.position;
        NetworkManager.players[65].trfm.position = redStart.position;

        NetworkManager.players[64].AssignDoppels(blueDoppels);
        NetworkManager.players[65].AssignDoppels(redDoppels);
    }

    void ClearEntities()
    {
        if (blueDoppels[0]) { blueDoppels[0].hpScript.End(); }
        if (blueDoppels[1]) { blueDoppels[1].hpScript.End(); }
        if (redDoppels[0]) { redDoppels[0].hpScript.End(); }
        if (redDoppels[1]) { redDoppels[1].hpScript.End(); }

        teamBlue.Clear();
        teamRed.Clear();

        for (ushort i = 1000; i < 1004; i++)
        {
            if (NetworkManager.networkObjects.ContainsKey(i)) { NetworkManager.networkObjects.Remove(i); }
        }        
    }

    int matchState;
    const int STARTING = 0, IN_PROGRESS = 1, CONCLUDED = 2;
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Return))
        {
            buffer3[FUNCTION_ID] = UPDATE_EVENT_KEY;
            NetworkManager.Send(buffer3);
            OnEventKey();
        }

        if (Input.GetKeyDown(KeyCode.Backspace))
        {
            CreateDoppels();
        }
    }

    void OnEventKey()
    {
        if (matchState == STARTING)
        {
            StartMatch();
            matchState = IN_PROGRESS;
        }
        else
        {
            ClearEntities();
            PlayerController.self.ClearEndText();
            matchState = STARTING;
        }
    }

    byte[] buffer3 = new byte[3];
    const int FUNCTION_ID = 2;
    const int UPDATE_EVENT_KEY = 0;
    public override void NetworkUpdate(byte[] buffer)
    {
        if (buffer[FUNCTION_ID] == UPDATE_EVENT_KEY) { OnEventKey(); }
    }
}

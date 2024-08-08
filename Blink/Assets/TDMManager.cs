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

    [SerializeField] GameObject doppelPointersParent;
    [SerializeField] DoppelPointer[] doppelPointers;

    [SerializeField] GameObject TutorialMap, TDMMap;

    static int teamSize;

    // Start is called before the first frame update
    new void Start()
    {

        base.Start();
        NetworkManager.networkObjects.Add(1, GetComponent<TDMManager>());

        blueDoppels = new Doppel[2];
        redDoppels = new Doppel[2];

        buffer3 = new byte[3];
        NetworkManager.SetBufferUShort(buffer3, 1);

        teamSize = 3;
        PlayerController.self.doppels = new Doppel[teamSize];
        blueDoppels = new Doppel[teamSize];
        redDoppels = new Doppel[teamSize];
    }

    #region MATCH_START

    void StartMatch()
    {
        NetworkManager.players[64].gamemode = PlayerController.GM_TDM;
        NetworkManager.players[65].gamemode = PlayerController.GM_TDM;

        TutorialMap.SetActive(false);
        TDMMap.SetActive(true);

        doppelPointersParent.SetActive(true);

        CreateDoppels();
        SetTeams();
    }

    void CreateDoppels()
    {
        for (int i = 0; i < teamSize; i++)
        {
            blueDoppels[i] = Instantiate(doppel, Vector3.zero, Quaternion.identity).GetComponent<Doppel>();
            teamBlue.Add(blueDoppels[i].posTracker);
            blueDoppels[i].AssignObjID((ushort)(1000 + i));            

            redDoppels[i] = Instantiate(doppel, Vector3.zero, Quaternion.identity).GetComponent<Doppel>();
            teamRed.Add(redDoppels[i].posTracker);
            redDoppels[i].AssignObjID((ushort)(1100 + i));

            if (PlayerController.playerObjID == 64) { doppelPointers[i].doppel = blueDoppels[i].trfm; }
            else { doppelPointers[i].doppel = redDoppels[i].trfm; }
        }

        blueDoppels[0].trfm.position = blueStart.position + Vector3.right * 5;
        blueDoppels[1].trfm.position = blueStart.position + Vector3.right * -5;
        blueDoppels[2].trfm.position = blueStart.position + Vector3.forward * -5;

        redDoppels[0].trfm.position = redStart.position + Vector3.right * 5;
        redDoppels[1].trfm.position = redStart.position + Vector3.right * -5;
        redDoppels[2].trfm.position = redStart.position + Vector3.forward * 5;
    }

    PlayerController localPlayer, opposingPlayer;
    void SetTeams()
    {
        NetworkManager.players[64].AssignTeam(TEAM_BLUE);
        NetworkManager.players[65].AssignTeam(TEAM_RED);

        teamBlue.Add(NetworkManager.players[64].posTracker);
        teamRed.Add(NetworkManager.players[65].posTracker);

        NetworkManager.players[64].opponents = teamRed;
        NetworkManager.players[65].opponents = teamBlue;

        for (int i = 0; i < teamSize; i++)
        {
            blueDoppels[i].Init(TEAM_BLUE, NetworkManager.players[64], teamRed, PlayerController.playerObjID == 64, i);
            redDoppels[i].Init(TEAM_RED, NetworkManager.players[64], teamBlue, PlayerController.playerObjID == 65, i);
        }

        NetworkManager.players[64].trfm.position = blueStart.position;
        NetworkManager.players[65].trfm.position = redStart.position;

        NetworkManager.players[64].AssignDoppels(blueDoppels);
        NetworkManager.players[65].AssignDoppels(redDoppels);
    }

    #endregion

    public void RevealOpponents(bool reveal)
    {
        for (int i = 0; i < teamSize; i++)
        {
            if (PlayerController.playerObjID == 64 && redDoppels[i]) { redDoppels[i].beacon.SetActive(reveal); }
            if (PlayerController.playerObjID == 65 && blueDoppels[i]) { blueDoppels[i].beacon.SetActive(reveal); }
        }
        if (PlayerController.playerObjID == 64) { NetworkManager.players[65].beacon.SetActive(reveal); }
        if (PlayerController.playerObjID == 65) { NetworkManager.players[64].beacon.SetActive(reveal); }
    }

    void ClearEntities()
    {
        for (int i = 0; i < teamSize; i++)
        {
            if (blueDoppels[i]) { blueDoppels[i].hpScript.End(); }
            if (redDoppels[i]) { redDoppels[i].hpScript.End(); }
        }

        teamBlue.Clear();
        teamRed.Clear();

        doppelPointersParent.SetActive(false);

        for (ushort i = 0; i < teamSize; i++)
        {
            if (NetworkManager.networkObjects.ContainsKey((ushort)(1000 + i))) { NetworkManager.networkObjects.Remove((ushort)(1000 + i)); }
            if (NetworkManager.networkObjects.ContainsKey((ushort)(1100 + i))) { NetworkManager.networkObjects.Remove((ushort)(1100 + i)); }
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
            RevealOpponents(true);
        }
    }

    void OnEventKey()
    {
        if (matchState == STARTING)
        {
            HealLocalTeam(999);
            StartMatch();
            PlayerController.self.ClearEndText();
            matchState = IN_PROGRESS;
        }
        else
        {
            HealLocalTeam(999);
            ClearEntities();
            PlayerController.self.ClearEndText();
            matchState = STARTING;
        }
    }

    #region GAMEPLAY

    public static void HealLocalTeam(ushort amount)
    {
        PlayerController.self.hpScript.Heal(amount, HPEntity.SYNC_HP, Tools.RandomEventID());
        for (int i = 0; i < teamSize; i++)
        {
            if (PlayerController.self.doppels[i]) { PlayerController.self.doppels[i].hpScript.Heal(amount, HPEntity.SYNC_HP, Tools.RandomEventID()); }
        }        
    }

    #endregion

    byte[] buffer3 = new byte[3];
    const int FUNCTION_ID = 2;
    const int UPDATE_EVENT_KEY = 0;
    public override void NetworkUpdate(byte[] buffer)
    {
        if (buffer[FUNCTION_ID] == UPDATE_EVENT_KEY) { OnEventKey(); }
    }
}

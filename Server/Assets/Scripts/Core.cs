using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public enum ServerEventType
{
	//called when the player has been authenticated
	login = 0,
	
	//in game high frequency position informations
	position = 1,
	
	//simple chat messages (if the client has joined a channel)
	chat = 2, 
	
	//private messages
	pm = 3,
	
	//event called when a player joins a channel I am in
	playerJoin = 4,
	
	//event called when a player leaves a channel I am in
	playerLeave = 5,
	
	//all "heavier" messages that are specific to custom situations
	custom = 6,
	
	//server messages 
	serverMessage = 7
}

public class Core : MonoBehaviour 
{
	
	//all connected players will be listed here.
	public Dictionary<int, Player> players = new Dictionary<int, Player>();
	
	//index players who have a username to avoid having the same name in the server
	Dictionary<string, int> playersByName = new Dictionary<string, int>();
	
	//all active channels are listed here:
	public Dictionary<int, Channel> channels = new Dictionary<int, Channel>();
	
	//index channels to avoid having the same channel name in the server
	Dictionary<string, int> channelsByName = new Dictionary<string, int>();
	
	//all active games are listed here:
	public Dictionary<int, GameRoom> games = new Dictionary<int, GameRoom>();
	
	void Start () 
	{
		Network.InitializeSecurity();
		Network.InitializeServer(int.MaxValue, 9999);
	}
	
	//called when a player connects to the server, the first thing we do is create a new player object and assign this player a unique session id
	void OnPlayerConnected(NetworkPlayer player) 
	{
        Player newPlayer = new Player(this, player);
		players.Add(player.GetHashCode(), newPlayer);
    }
	
	void OnPlayerDisconnected(NetworkPlayer player) 
	{
		Network.RemoveRPCs(player);
        //Network.DestroyPlayerObjects(player);
		Player myPlayer = players[player.GetHashCode()];
		myPlayer.Channel.removePlayer(myPlayer);
		
    }
	
	//used to handle client requests
	[RPC]
	void onClientEvent(object[] parameters, NetworkMessageInfo sender)
	{
		//the type of event sent by the client
		byte eventType = (byte) parameters[0];
		
		//the Player object associated to this client's instance
		Player myPlayer = players[sender.networkView.GetHashCode()];
		
		//the specific data
		object[] data = (object[]) parameters[1];
		
		//login requests
		if(eventType==(byte)ServerEventType.login)
		{
			string username = (string) data[0];
			
			//no secure authentification for now...
			//string password = (string) data[1];
			
			myPlayer.Name = username;
			
			object[] playerInfos = new object[]
			{
				myPlayer.Name, myPlayer.Id
			};
		}
		
		//channel join requests
		if(eventType==(byte)ServerEventType.playerJoin)
		{
			if(data[0].GetType().Equals(typeof(int)))
			{
				int channelId = (int) data[0];
				
				try
				{
					channels[channelId].addPlayer(myPlayer);
				}
				catch
				{
					object[] message = new object[]
					{
						"Channel not found!"
					};
					myPlayer.Send(ServerEventType.serverMessage, message);
				}
			}
			else
			{
				string channelName = (string) data[0];
				
				try
				{
					channels[channelsByName[channelName]].addPlayer(myPlayer);
				}
				catch
				{
					object[] message = new object[]
					{
						"Channel not found!"
					};
					myPlayer.Send(ServerEventType.serverMessage, message);
				}
			}
		}
	}
}

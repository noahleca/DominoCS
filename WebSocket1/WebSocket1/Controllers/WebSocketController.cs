using Microsoft.Web.WebSockets;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web;
using System.Web.Http;
using System.Web.UI.WebControls;
using WebSocket1.Models;

namespace WebSocket1.Controllers
{
    public class WebSocketController : ApiController
    {

        public HttpResponseMessage Get(string nom)
        {
            HttpContext.Current.AcceptWebSocketRequest(new SocketHandler(nom)); return new HttpResponseMessage(HttpStatusCode.SwitchingProtocols);
        }

        private class SocketHandler : WebSocketHandler
        {
            private static readonly WebSocketCollection Sockets = new WebSocketCollection();
            private static List<string> tiles = Game.getRandomRealTiles();
            private static string[,] VirtualTiles = Game.getVirtualTiles();
            private static int nPlayers = 0;
            private readonly string _nom;
            private static string board = "";
            private static int lowestTotal = int.MaxValue;
            private static string clientWithLowestTotal = "";
            private static List<(string client, int total)> receivedRecords = new List<(string, int)>();

            //variables estáticas de acciones de los mensajes entre el cliente el servidor
            //todo lo que se cambie aquí se deberá cambiar en el cliente y viceversa.
            private const string GET_NUM_PLAYER = "dar numero de jugador";
            private const string SALA_LLENA = "sala llena";
            private const string GET_TILES = "pedir fichas";
            private const string GET_TILE = "obtener ficha";
            private const string RELOAD_BOARD = "recargar domino";
            private const string CHANGE_TURN = "cambiar turno";
            private const string BYE = "adios";
            private const string RIGHT = "R";
            private const string CAN_START = "puedo empezar";
            private const string GET_TOTAL = "GetTotal";
            private const string LOWEST_TOTAL = "LowestTotal";

            private const string LOWEST_INTO_CLIENT = "LowesIntoClient";

            public SocketHandler(string nom)
            {
                _nom = nom;
            }

            public override void OnOpen()
            {
                if (nPlayers < 4)
                {
                    Sockets.Add(this);
                    nPlayers++;
                    Send($"{GET_NUM_PLAYER},{nPlayers}");
                }
                else
                {
                    Send($"{SALA_LLENA},");
                    OnClose();
                }
            }
            public override void OnMessage(string message)
            {
                GetMessageFromClient(message.Split(','));
            }

            public void GetMessageFromClient(string[] message)
            {
                string action = message[0];
                string content = message[1];

                switch (action)
                {
                    case GET_TILES:
                        GiveTiles();
                        break;
                    case RELOAD_BOARD:
                        ReloadBoard(content, message[2], int.Parse(message[3]), message[4]);
                        break;
                    case CHANGE_TURN:
                        ChangeTurn(GetNextPlayer(int.Parse(content)));
                        break;
                    case CAN_START:
                        Sockets.Broadcast($"{CAN_START},");
                        break;
                    case GET_TOTAL:
                        GetTotalPoints(content, message[2]);
                        break;
                }
            }
            public void GetTotalPoints(string message, string username)
            {
                int tempTotal = int.Parse(message);
                receivedRecords.Add((username, tempTotal));
                if (receivedRecords.Count == 4)
                {
                    LowestPlayer lowestNumber = GetLowestTotal();
                    Sockets.Broadcast($"{LOWEST_INTO_CLIENT},{lowestNumber.points}, {lowestNumber.name}");
                }
            }
            public LowestPlayer GetLowestTotal()
            {
                LowestPlayer lp = new LowestPlayer("", 0);
                int lowestTotal = int.MaxValue;
                foreach (var record in receivedRecords)
                {
                    if (lowestTotal > record.total)
                    {
                        lowestTotal = record.total;
                        lp.name = record.client;
                        lp.points = record.total;
                    }
                }
                return lp;
            }
            public void ChangeTurn(int nextPlayer)
            {
                Sockets.Broadcast($"{CHANGE_TURN},{nextPlayer}");
            }

            public int GetNextPlayer(int currentPlayer)
            {
                return currentPlayer < nPlayers ? currentPlayer + 1 : 1;
            }

            public void ReloadBoard(string tile, string side, int player, string winer)
            {
                board = side.Equals(RIGHT) ? board + tile : tile + board;
                string messageWiner = !String.IsNullOrEmpty(winer) ? $"{_nom} won the game!" : "";
                if (BoardIsBlocked())
                {
                    messageWiner = "BoardClosed";
                }
                Sockets.Broadcast($"{RELOAD_BOARD},{board},{GetNextPlayer(player)},{messageWiner}");
            }

            public bool BoardIsBlocked()
            {

                if (board.Length >= 7)
                {
                    string leftTile, rightTile;
                    GetBoardWings(board, out leftTile, out rightTile);

                    int[] valoresLeft, valoresRight;
                    valoresLeft = GetSides(leftTile);
                    valoresRight = GetSides(rightTile);

                    if (valoresLeft[0] == valoresRight[1])
                    {
                        int valor = valoresLeft[0];
                        int fichasCount = 0;
                        string[] tableroArray = BoardToArray(board);
                        foreach (string ficha in tableroArray)
                        {
                            int[] valoresTemp = GetSides(ficha);
                            if (valoresTemp[0] == valor || valoresTemp[1] == valor)
                            {
                                fichasCount++;
                            }
                        }
                        if (fichasCount == 7)
                        {
                            return true;
                        }
                    }
                }
                return false;
            }

            public void GetBoardWings(string board, out string leftTile, out string rightTile)
            {
                leftTile = board.Substring(0, 2);
                rightTile = board.Substring(board.Length - 2, 2);
            }

            public string[] BoardToArray(string board)
            {
                List<string> tableroList = new List<string>();
                for (int i = 0; i < board.Length; i += 2)
                {
                    string tile = board.Substring(i, 2);
                    tableroList.Add(tile);
                }
                return tableroList.ToArray();
            }

            public static int[] GetSides(string ficha)
            {
                for (int i = 0; i < 7; i++)
                {
                    for (int j = 0; j < 7; j++)
                    {
                        if (VirtualTiles[i, j].Equals(ficha))
                        {
                            return new int[] { i, j };
                        }
                    }
                }
                return new int[] { -1, -1 };
            }

            public void GiveTiles()
            {
                int i = 0;
                while (i < tiles.Count && i < 7)
                {
                    Send($"{GET_TILE},{tiles.ElementAt(i)}");
                    i++;
                }
                tiles.RemoveRange(0, i);
            }
            public override void OnClose()
            {
                Sockets.Remove(this);
                Sockets.Broadcast($"{BYE},{_nom} left the game. (no se puede jugar porque {_nom} se ha ido...) ");
            }
        }
    }
}

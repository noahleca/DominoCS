using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace WebSocketClientGraphicInterface.Controller
{
    public class Controller
    {
        Form1 f;
        CancellationTokenSource cts;
        ClientWebSocket socket;
        private int buttonsListPosition = 0;
        private List<Button> Buttons;
        private int nPlayer = 0;
        private bool firstPlayer = false;
        private static string[,] VirtualTiles = GetVirtualTiles();
        private bool connected = false;
        private bool canPlay = false;

        private const string msgError6x6 = "¡Para empezar necesitas tirar el doble 6!";
        private const string msgErrorCannotThrowYet = "Ahora no puedes tirar una ficha.";
        private const string msgErrorCannotPass = "Aún no pasar el turno...";
        private const string msgErrorRoomFull = "La sala está llena...";
        private const string msgErrorCannotPutTile = "No puedes tirar esta ficha...";

        /*Variables estáticas de acciones para los mensajes entre el cliente y el servidor.*/
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
        private const string LOWEST_INTO_CLIENT = "LowesIntoClient";
        public Controller()
        {
            f = new Form1();
            InitListeners();
            LoadData();
            f.board.Text = "";
            f.TURN.Visible = false;
            Buttons = f.Controls.OfType<Button>().Where(btn => btn.Name.StartsWith("tile")).ToList();
            Application.Run(f);
        }
        void InitListeners()
        {
            f.connect.Click += Btn_ConnectClicked;
            f.tile1.MouseDown += TileClicked;
            f.tile2.MouseDown += TileClicked;
            f.tile3.MouseDown += TileClicked;
            f.tile4.MouseDown += TileClicked;
            f.tile5.MouseDown += TileClicked;
            f.tile6.MouseDown += TileClicked;
            f.tile7.MouseDown += TileClicked;
            f.pasar.Click += PassClicked;
        }

        void LoadData()
        {
            f.urlServer.Text = "localhost:44330";
            f.userName.Text = Environment.UserName;
        }

        async void Btn_ConnectClicked(object sender, EventArgs e)
        {
            if (f.connect.Text.Equals("PLAY GAME"))
            {
                await ConnectarAsync();
            }
            else
            {
                Disconnect();
            }
        }

        public async Task ConnectarAsync()
        {
            cts = new CancellationTokenSource();
            socket = new ClientWebSocket();
            try
            {
                await socket.ConnectAsync(new Uri($"wss://{f.urlServer.Text}/api/websocket?nom={f.userName.Text}"), cts.Token);
                if (socket.State.ToString() == "Open")
                {
                    f.connect.Text = "LEAVE GAME";
                    f.connect.BackColor = Color.Red;
                    f.urlServer.Enabled = false;
                    f.userName.Enabled = false;
                    connected = true;
                    WriteMessages($"{GET_TILES},");
                    await ListenMessages(cts, socket);
                }
            }
            catch (Exception) { }
        }

        void Disconnect()
        {
            f.connect.Text = "PLAY GAME";
            f.connect.BackColor = Color.LightGreen;
            f.urlServer.Enabled = true;
            f.userName.Enabled = true;
            connected = false;
            try
            {
                cts.Cancel();
            }
            catch (Exception) { }
        }

        public void GetMessageFromServer(string[] message)
        {
            string action = message[0];
            string content = message[1];

            switch (action)
            {
                case GET_NUM_PLAYER:
                    nPlayer = int.Parse(content);
                    WriteMessageInTextList($"Bienvenido {f.userName.Text}! Eres el jugador numero {nPlayer}.");
                    break;
                case SALA_LLENA:
                    MessageBox.Show(msgErrorRoomFull, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    Disconnect();
                    break;
                case GET_TILE:
                    GetTile(content);
                    break;
                case RELOAD_BOARD:
                    ReloadBoard(content, int.Parse(message[2]), message[3]);
                    break;
                case CHANGE_TURN:
                    SetTurn(int.Parse(content));
                    break;
                case BYE:
                    WriteMessageInTextList(content);
                    break;
                case CAN_START:
                    canPlay = true;
                    break;
                case LOWEST_INTO_CLIENT:
                    MessageBox.Show("Ha ganado la partida:" + message[2] + " con un total de: " + message[1] + " puntos restantes. ¡Gracias por jugar!");
                    Disconnect();
                    f.connect.Enabled = false;
                    break;
                default:

                    break;
            }
        }
        public int GetTotalPoints()
        {
            int totalPoints = 0;
            foreach (Button tileButton in f.Controls.OfType<Button>().Where(btn => btn.Name.StartsWith("tile") && btn.Enabled))
            {
                int[] tilePoints = GetSides(tileButton.Text);
                totalPoints += tilePoints[0] + tilePoints[1];
            }
            return totalPoints;
        }
        public void SetTurn(int nextPlayer)
        {
            if (nPlayer == nextPlayer)
            {
                f.TURN.Visible = true;
            }
        }

        public void WriteMessageInTextList(string message)
        {
            f.llista.Items.Add(message);
        }

        void TileClicked(object sender, MouseEventArgs e)
        {
            if (connected && canPlay && f.TURN.Visible)
            {
                if (firstPlayer)
                {
                    if ((sender as Button).Text.Equals("🂓"))
                    {
                        string side = e.Button == MouseButtons.Right ? RIGHT : "";


                        firstPlayer = false;
                        PutTile((sender as Button), side, "");
                    }
                    else
                    {
                        MessageBox.Show(msgError6x6, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
                else
                {
                    string side = e.Button == MouseButtons.Right ? RIGHT : "";
                    string boardTile = side == RIGHT ? f.board.Text.Substring(f.board.Text.Length - 2, 2) : f.board.Text.Substring(0, 2);
                    PutTile((sender as Button), side, boardTile);
                }
            }
            else
            {
                MessageBox.Show(msgErrorCannotThrowYet, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        static String[,] GetVirtualTiles()
        {
            String[,] arr2 = new String[7, 7];
            byte[] arr = Encoding.Unicode.GetBytes("\U0001F031");
            for (int i = 0; i < 7; i++)
            {
                for (int j = 0; j < 7; j++)
                {
                    string ficha = Encoding.Unicode.GetString(arr);
                    if (i == j)
                    {
                        arr[2] += 50;
                        ficha = Encoding.Unicode.GetString(arr);
                        arr[2] -= 50;
                    }
                    arr2[i, j] = ficha;
                    arr[2]++;
                }
            }
            return arr2;
        }


        void PutTile(Button btn, string side, string boardTile)
        {
            bool canPutTile = false;
            string tile = btn.Text;
            if (!String.IsNullOrEmpty(boardTile))
            {
                int[] positionsOfPlayerTile = GetSides(tile);
                int[] positionsOfTileInBoard = GetSides(boardTile);
                int numberToCompare = side == RIGHT ? positionsOfTileInBoard[1] : positionsOfTileInBoard[0];
                if (side == RIGHT)
                {
                    if (positionsOfPlayerTile[0] == numberToCompare)
                    {
                        canPutTile = true;
                    }
                    else if (positionsOfPlayerTile[1] == numberToCompare)
                    {
                        tile = VirtualTiles[positionsOfPlayerTile[1], positionsOfPlayerTile[0]];
                        canPutTile = true;
                    }
                    else
                    {
                        boardTile = f.board.Text.Substring(0, 2);
                        positionsOfTileInBoard = GetSides(boardTile);
                        numberToCompare = positionsOfTileInBoard[0];

                        if (positionsOfPlayerTile[1] == numberToCompare)
                        {
                            side = "";
                            canPutTile = true;
                        }
                        else if (positionsOfPlayerTile[0] == numberToCompare)
                        {

                            side = "";
                            tile = VirtualTiles[positionsOfPlayerTile[1], positionsOfPlayerTile[0]];
                            canPutTile = true;
                        }
                    }
                }
                else
                {
                    if (positionsOfPlayerTile[1] == numberToCompare)
                    {
                        canPutTile = true;
                    }
                    else if (positionsOfPlayerTile[0] == numberToCompare)
                    {
                        tile = VirtualTiles[positionsOfPlayerTile[1], positionsOfPlayerTile[0]];
                        canPutTile = true;
                    }
                    else
                    {
                        boardTile = f.board.Text.Substring(f.board.Text.Length - 2, 2);
                        positionsOfTileInBoard = GetSides(boardTile);
                        numberToCompare = positionsOfTileInBoard[1];
                        if (positionsOfPlayerTile[0] == numberToCompare)
                        {
                            side = RIGHT;
                            canPutTile = true;
                        }
                        else if (positionsOfPlayerTile[1] == numberToCompare)
                        {
                            side = RIGHT;
                            tile = VirtualTiles[positionsOfPlayerTile[1], positionsOfPlayerTile[0]];
                            canPutTile = true;
                        }
                    }
                }
            }
            else
            {
                canPutTile = true;
            }
            if (canPutTile)
            {
                btn.Enabled = false;
                btn.BackColor = Color.Transparent;
                string winer = YouWin() ? "win" : "";
                f.TURN.Visible = false;
                WriteMessages($"{RELOAD_BOARD},{tile},{side},{nPlayer},{winer}");
            }
            else
            {
                MessageBox.Show(msgErrorCannotPutTile, "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
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

        bool YouWin()
        {
            return Buttons.Where(b => b.Enabled).Count() == 0;
        }

        public void PassClicked(object sender, EventArgs e)
        {
            if (connected && canPlay && f.TURN.Visible && !firstPlayer)
            {
                f.TURN.Visible = false;
                WriteMessages($"{CHANGE_TURN},{nPlayer}");
            }
            else
            {
                MessageBox.Show(msgErrorCannotPass, "No puedes pasar el turno.", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        void ReloadBoard(string board, int nextPlayer, string messageWiner)
        {
            f.board.Text = board;
            if (!String.IsNullOrEmpty(messageWiner))
            {
                if (messageWiner.Equals("BoardClosed"))
                {
                    WriteMessages($"{GET_TOTAL},{GetTotalPoints()},{f.userName.Text}");
                }
                else
                {
                    MessageBox.Show(messageWiner, "GAME OVER", MessageBoxButtons.OK, MessageBoxIcon.Information, MessageBoxDefaultButton.Button1, MessageBoxOptions.DefaultDesktopOnly);
                    Disconnect();
                }
            }
            else
            {
                SetTurn(nextPlayer);
            }
        }

        public void GetTile(string tile)
        {
            if (tile.Equals("🂓"))
            {
                firstPlayer = true;
                f.TURN.Visible = true;
                Buttons[buttonsListPosition].BackColor = Color.LightGreen;
            }
            Buttons[buttonsListPosition].Text = tile;
            bool lastTileOfLastPlayer = nPlayer == 4 && buttonsListPosition == 6;
            if (lastTileOfLastPlayer)
            {
                WriteMessages($"{CAN_START},");
            }
            buttonsListPosition++;
        }

        public async Task ListenMessages(CancellationTokenSource cts, ClientWebSocket socket)
        {
            var s = new byte[256];
            var rcvBuffer = new ArraySegment<byte>(s);
            while (true)
            {
                WebSocketReceiveResult rcvResult = await socket.ReceiveAsync(rcvBuffer, cts.Token);
                byte[] msgBytes = rcvBuffer.Skip(rcvBuffer.Offset).Take(rcvResult.Count).ToArray();
                string rcvMsg = Encoding.UTF8.GetString(msgBytes);
                GetMessageFromServer(rcvMsg.Split(','));
            }
        }

        public void WriteMessages(string message)
        {
            byte[] sendBytes = Encoding.UTF8.GetBytes(message);
            var sendBuffer = new ArraySegment<byte>(sendBytes);
            socket.SendAsync(sendBuffer, WebSocketMessageType.Text, true, cts.Token);
        }
    }
}

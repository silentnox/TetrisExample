using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Linq;

enum Direction {
	None,
	Left,
	Right,
	Up,
	Down
}

enum Command {
	None,
	MoveLeft,
	MoveRight,
	MoveDown,
	RotateCW,
	RotateCCW,
	Jammed
}

class Figure {
	public int[,] shape = null;
	public int dimension = 0;
	public int sizeX = 0;
	public int sizeY = 0;

	public Vector2Int position = new Vector2Int(-1, -1);

	public Figure( int sizeX, int sizeY, string data ) {
		dimension = Math.Max(sizeX,sizeY);
		this.sizeX = sizeX;
		this.sizeY = sizeY;
		shape = new int[dimension,dimension];

		int index = 0;
		foreach( char s in data) {
			shape[index%sizeX,index/sizeX] = (s == '#') ? index % 2 + 2 : 0;
			index++;
		}
	}

	public Figure(int sizeX, int sizeY) {
		dimension = Math.Max(sizeX, sizeY);
		this.sizeX = sizeX;
		this.sizeY = sizeY;
		shape = new int[dimension, dimension];
	}

	public Figure Copy() {
		return (Figure)this.MemberwiseClone();
	}

	public void Move(Vector2Int offset) {
		position += offset;
	}

	public Figure RotateCW() {
		Figure newFigure = new Figure(dimension, dimension);
		newFigure.position = position;
		for (int i = 0; i < shape.GetLength(0); i++) {
			for (int j = 0; j < shape.GetLength(1); j++) {
				newFigure.shape[i, j] = shape[dimension - j - 1, i];
			}
		}
		return newFigure;
	}

	public Figure RotateCCW() {
		Figure newFigure = new Figure(dimension, dimension);
		newFigure.position = position;
		for (int i = 0; i < shape.GetLength(0); i++) {
			for (int j = 0; j < shape.GetLength(1); j++) {
				newFigure.shape[i, j] = shape[j, dimension - i - 1];
			}
		}
		return newFigure;
	}
}

interface IGameMode {
	int[,] GetField();
	Vector2Int GetFieldSize();
	Figure GetActiveFigure();
	bool IsGameOver();
	void InitGameMode();
	void ProcessUserCmd(Command cmd);
	void Tick();
}

class GameMode1 : IGameMode {
	static readonly Figure[] figures = new Figure[] {
		new Figure(2,2,
			"##" +
			"##"
			),
		new Figure(3,2,
			"##_" +
			"_##"
			),
		new Figure(3,2,
			"_##" +
			"##_"
			),
		new Figure(3,2,
			"__#" +
			"###"
			),
		new Figure(3,2,
			"__#" +
			"###"
			),
		new Figure(4,1,
			"####"
			),
		new Figure(3,2,
			"_#_" +
			"###"
			),
	};

	readonly double[] figureSpawnChance = new double[]{ 10, 15, 15, 15, 15, 10, 20 };

	protected int score = 0;

	protected int[,] field = null;
	//readonly Vector2Int fieldSize = new Vector2Int(10, 20);

	protected virtual Vector2Int fieldSize {
		get {
			return new Vector2Int(10, 20);
		}
	}

	protected bool gameOver = false;

	protected Figure figure = null;

	protected System.Random random = new System.Random();

	protected static Vector2Int DirectionToOffset(Direction dir) {
		switch (dir) {
			case Direction.Left:
			return new Vector2Int(-1, 0);
			case Direction.Right:
			return new Vector2Int(1, 0);
			case Direction.Up:
			return new Vector2Int(0, -1);
			case Direction.Down:
			return new Vector2Int(0, 1);
			default:
			return new Vector2Int(0, 0);
		}
	}

	public GameMode1() {
		//fieldSize = new Vector2Int(10, 20);
	}

	virtual public bool IsGameOver() {
		return gameOver;
	}

	virtual public Vector2Int GetFieldSize() {
		return fieldSize;
	}

	virtual public int [,] GetField() {
		//int[,] fieldWithFigure = new int[fieldSize.x,fieldSize.y];
		int[,] fieldWithFigure = (int[,])field.Clone();
		UpdateFieldWithFigure(fieldWithFigure, figure);
		return fieldWithFigure;
	}

	virtual public Figure GetActiveFigure() {
		return figure;
	}

	virtual public void InitGameMode() {
		gameOver = false;
		score = 0;
		figure = null;
		field = new int[fieldSize.x, fieldSize.y];
	}

	virtual protected Figure[] GetFiguresList() {
		return figures;
	}

	virtual protected double[] GetFigureSpawnChances() {
		return figureSpawnChance;
	}

	virtual protected void UpdateFieldWithFigure(int[,] field, Figure figure) {
		if(figure == null) {
			return;
		}
		for (int i = 0; i < figure.dimension; i++) {
			for (int j = 0; j < figure.dimension; j++) {
				int fig = figure.shape[i, j];
				if(fig == 0) {
					continue;
				}
				//Vector2Int worldPos = new Vector2Int(figure.position.x + i, figure.position.y + j);
				Vector2Int worldPos = MapPointToField(new Vector2Int(figure.position.x + i, figure.position.y + j));
				if(IsOutOfBounds(worldPos)) {
					continue;
				}
				field[worldPos.x, worldPos.y] = fig;
			}
		}
	}

	virtual protected bool IsOutOfBounds(Vector2Int pos) {
		return pos.x < 0 || pos.y < 0 || pos.x >= fieldSize.x || pos.y >= fieldSize.y;
	}

	virtual protected Vector2Int MapPointToField(Vector2Int point) {
		return point;
	}

	virtual protected bool IsFigureStuck(Figure figure) {
		for (int i = 0; i < figure.dimension; i++) {
			for (int j = 0; j < figure.dimension; j++) {
				int fig = figure.shape[i, j];
				if (fig == 0) {
					continue;
				}
				//Vector2Int worldPos = new Vector2Int(figure.position.x + i, figure.position.y + j);
				Vector2Int worldPos = MapPointToField(new Vector2Int(figure.position.x + i, figure.position.y + j));

				if(IsOutOfBounds(worldPos)) {
					if(worldPos.y >= fieldSize.y) {
						return true;
					}
					else {
						continue;
					}
				}

				int fld = field[worldPos.x, worldPos.y];

				if (fig != 0 && fld != 0) {
					return true;
				}
			}
		}

		return false;
	}

	virtual protected bool IsFigureOutOfBounds(Figure figure) {
		for (int i = 0; i < figure.dimension; i++) {
			for (int j = 0; j < figure.dimension; j++) {
				if(figure.shape[i,j] == 0) {
					continue;
				}
				Vector2Int worldPos = new Vector2Int(figure.position.x + i, figure.position.y + j);

				if(IsOutOfBounds(worldPos)) {
					return true;
				}
			}
		}

		return false;
	}

	virtual protected void MoveFigure(Direction dir) {
		if (dir == Direction.None) return;
		if (figure == null) return;

		Vector2Int offset = DirectionToOffset(dir);

		figure.Move(offset);

		bool stuck = IsFigureStuck(figure);
		bool outOfBounds = IsFigureOutOfBounds(figure);

		if(stuck) {
			figure.Move(offset*-1);
			UpdateFieldWithFigure(field, figure);
			figure = null;
		}
		else if (outOfBounds) {
			figure.Move(offset * -1);
		}
	}

	virtual protected void RotateFigure(bool cw) {
		Figure newFigure = null;
		if(cw) {
			newFigure = figure.RotateCW();
		}
		else {
			newFigure = figure.RotateCCW();
		}
		if (!IsFigureStuck(newFigure) && !IsFigureOutOfBounds(newFigure)) {
			figure = newFigure;
		}
	}

	public void ProcessUserCmd(Command cmd) {
		if(gameOver) {
			return;
		}
		if(cmd == Command.None) {
			return;
		}

		if (cmd == Command.MoveLeft) {
			MoveFigure(Direction.Left);
		}
		else if (cmd == Command.MoveRight) {
			MoveFigure(Direction.Right);
		}
		else if (cmd == Command.MoveDown) {
			MoveFigure(Direction.Down);
		}
		else if (cmd == Command.RotateCW) {
			RotateFigure(true);
		}
		else if (cmd == Command.RotateCCW) {
			RotateFigure(false);
		}
	}

	static protected void CopyRow( int[,] src, int index, int[,] dst, int dstIndex) {
		for(int i = 0; i < src.GetLength(0); i++) {
			dst[i, dstIndex] = src[i, index];
		}
	}

	void EleminateRows() {
		int[,] newField = new int[fieldSize.x, fieldSize.y];
		int numNewRows = 0;
		for( int i = fieldSize.y-1;i >= 0; i--) {
			if(!ShouldEleminateRow(i)) {
				CopyRow(field, i, newField, fieldSize.y - numNewRows - 1);
				numNewRows += 1;
			}
		}

		field = newField;
	}

	virtual protected bool IsRowFull(int index) {
		for (int i = 0; i < fieldSize.x; i++) {
			if (field[i, index] == 0) {
				return false;
			}
		}
		return true;
	}

	virtual protected bool ShouldEleminateRow(int index) {
		return IsRowFull(index);
	}

	virtual protected int GetNextFigureType() {
		double chanceFactor = 100.0 / figureSpawnChance.Sum();

		double[] chances = GetFigureSpawnChances();

		double[] normChances = chances.Select((double value) => value / chanceFactor).ToArray();

		for (int i = 1; i < normChances.Length; i++) {
			normChances[i] += normChances[i - 1];
		}

		double roll = random.NextDouble() * 100.0;

		for( int i = 0; i < normChances.Length; i++) {
			if(roll < normChances[i]) {
				return i;
			}
		}

		return -1;
	}

	virtual protected void SpawnFigure() {
		if (figure != null) return;

		Figure[] figures = GetFiguresList();

		//int nextFigureType = random.Next(figures.Length-1);
		int nextFigureType = GetNextFigureType();
		figure = figures[nextFigureType].Copy();

		int offsetX = random.Next(fieldSize.x-figure.dimension);

		figure.position = new Vector2Int(offsetX, 0);

		if(IsFigureStuck(figure)) {
			gameOver = true;
		}
	}

	public virtual void Tick() {
		if(gameOver) {
			return;
		}
		//ProcessUserCmd(userCmd);
		MoveFigure(Direction.Down);
		SpawnFigure();
		EleminateRows();
	}
}

class GameMode2 : GameMode1 {
	static readonly Figure[] figures = new Figure[] {
		new Figure(3,3,
			"_#_" +
			"###" +
			"_#_"
			),
		new Figure(3,2,
			"###" +
			"#_#"
			),
		new Figure(3,3,
			"#__" +
			"##_" +
			"_##"
			),
	};

	readonly double[] figureSpawnChance = new double[] { 10, 15, 15, 15, 15, 10, 5, 5, 5, 5 };

	protected override Vector2Int fieldSize {
		get {
			return new Vector2Int(12, 20);
		}
	}

	public GameMode2() {
		//fieldSize = new Vector2Int(10, 20);
	}

	override protected Figure[] GetFiguresList() {
		return base.GetFiguresList().Concat(figures).ToArray();
	}

	override protected double[] GetFigureSpawnChances() {
		return figureSpawnChance;
	}

	override public void InitGameMode() {
		gameOver = false;
		score = 0;
		figure = null;
		field = new int[fieldSize.x, fieldSize.y];
	}

	override protected bool IsOutOfBounds(Vector2Int pos) {
		return pos.y < 0 || pos.y > fieldSize.y-1;
	}

	override protected Vector2Int MapPointToField(Vector2Int point) {
		if(point.x < 0) {
			return new Vector2Int(point.x + fieldSize.x,point.y);
		}
		if (point.x > fieldSize.x-1) {
			return new Vector2Int(point.x - fieldSize.x, point.y);
		}
		return point;
	}

	override protected bool ShouldEleminateRow(int index) {
		bool eleminate = false;
		if (!IsRowFull(index)) {
			return false;
		}
		if (index > 0) {
			eleminate |= IsRowFull(index - 1);
		}
		if (index < fieldSize.y - 1) {
			eleminate |= IsRowFull(index + 1);
		}

		return eleminate;
	}
}

class Tetris {

	const double frameInterval = 1/5.0;
	double frameTimer = 0;

	const int cellSize = 30;

	Command lastCommand = Command.None;

	Texture2D backgroundTexture = null;
	Texture2D figureTexture1 = null;
	Texture2D figureTexture2 = null;

	IGameMode gameMode = null;

	int currentGameMode = -1;

	bool paused = false;

	void ChangeGameMode(int newMode) {
		if (newMode == 0) {
			gameMode = new GameMode1();
		}
		else if(newMode == 1) {
			gameMode = new GameMode2();
		}
		currentGameMode = newMode;
		gameMode.InitGameMode();
	}

	void Restart() {
		ChangeGameMode(currentGameMode);
	}

	public Tetris() {
		ChangeGameMode(0);
	}

	public void InitTetris() {
		backgroundTexture = new Texture2D(1, 1);
		figureTexture1 = new Texture2D(1, 1);
		figureTexture2 = new Texture2D(1, 1);

		backgroundTexture.SetPixel(0, 0, Color.white);
		figureTexture1.SetPixel(0, 0, Color.Lerp(Color.gray, Color.black, 0.3f));
		figureTexture2.SetPixel(0, 0, Color.Lerp(Color.gray, Color.white, 0.3f));
		//figureTexture1.SetPixel(0, 0, Color.black);
		//figureTexture2.SetPixel(0, 0, Color.white);

		backgroundTexture.Apply();
		figureTexture1.Apply();
		figureTexture2.Apply();
	}

	void DrawGrid( Vector2Int offset, int[,] grid ) {

		Texture2D[] mosaic = new Texture2D[] {null, backgroundTexture, figureTexture1, figureTexture2};

		for (int i = 0; i < grid.GetLength(0); i++) {
			for (int j = 0; j < grid.GetLength(1); j++) {
				Rect pos = new Rect(offset.x + i * cellSize, offset.y + j * cellSize, cellSize, cellSize);
				Texture2D tex = mosaic[grid[i, j]];
				if (tex != null) {
					GUI.DrawTexture(pos, tex);
				}
			}
		}
	}

	public void RenderFrame() {

		Vector2Int fieldSize = gameMode.GetFieldSize();
		int [,] field = gameMode.GetField();

		Vector2Int offset = new Vector2Int(Screen.width / 2 - fieldSize.x*cellSize/2, Screen.height / 2 - fieldSize.y * cellSize / 2);

		Rect backgroundRect = new Rect(offset.x, offset.y, fieldSize.x * cellSize, fieldSize.y * cellSize);
		GUI.DrawTexture(backgroundRect, backgroundTexture);

		DrawGrid(offset, field);

		Rect labelRect = backgroundRect;
		labelRect.center += new Vector2(labelRect.width+10, 0);

		GUIStyle labelStyle = new GUIStyle();
		labelStyle.fontSize = 20;
		labelStyle.normal.textColor = Color.white;

		GUI.Label(labelRect,
			"Controls:\n" +
			"1,2 - Switch mode\n" +
			"P - pause, R - restart\n" +
			"Left, Right arrows - move horizontally\n" +
			"Up arrow or X - rotate clockwise\n" +
			"Left control or Z - rotate counter-clockwise\n" +
			"Down arrow - force land\n",
			labelStyle);
	}

	public void RunFrame() {

		if (Input.GetKeyDown(KeyCode.Alpha1)) {
			ChangeGameMode(0);
			return;
		}

		if (Input.GetKeyDown(KeyCode.Alpha2)) {
			ChangeGameMode(1);
			return;
		}

		if (Input.GetKeyDown(KeyCode.R)) {
			Restart();
			return;
		}

		if (Input.GetKeyDown(KeyCode.P)) {
			paused = !paused;
		}

		if(paused) {
			return;
		}

		Command command = GetCommandFromInput();
		//lastCommand = command != Command.None ? command : lastCommand;
		gameMode.ProcessUserCmd(command);
		frameTimer -= Time.deltaTime;
		if (frameTimer < 0) {
			frameTimer = frameInterval;
			//gameMode.ProcessUserCmd(lastCommand);
			//lastCommand = Command.None;
			gameMode.Tick();
		}
	}

	static Command GetCommandFromInput() {
		int numKeys = 0;
		Command cmd = Command.None;

		if(Input.GetKeyDown(KeyCode.LeftArrow)) {
			numKeys += 1;
			cmd = Command.MoveLeft;
		}
		if (Input.GetKeyDown(KeyCode.RightArrow)) {
			numKeys += 1;
			cmd = Command.MoveRight;
		}
		if (Input.GetKeyDown(KeyCode.UpArrow) || Input.GetKeyDown(KeyCode.X)) {
			numKeys += 1;
			cmd = Command.RotateCW;
		}
		if (Input.GetKeyDown(KeyCode.LeftControl) || Input.GetKeyDown(KeyCode.Z)) {
			numKeys += 1;
			cmd = Command.RotateCCW;
		}
		if (Input.GetKeyDown(KeyCode.DownArrow) || Input.GetKeyDown(KeyCode.Space)) {
			numKeys += 1;
			cmd = Command.MoveDown;
		}

		if(numKeys == 1) {
			return cmd;
		}
		else if(numKeys > 1) {
			return Command.Jammed;
		}
		else {
			return Command.None;
		}
	}
}

public class TetrisEngine : MonoBehaviour {
	Tetris tetris = new Tetris();

    // Start is called before the first frame update
    void Start() {
		tetris.InitTetris();
    }

    // Update is called once per frame
    void Update() {
		tetris.RunFrame();
    }

	private void OnGUI() {
		tetris.RenderFrame();
	}
}

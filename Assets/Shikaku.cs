using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using KmHelper;
using Rnd = UnityEngine.Random;
using Assets;

public class Shikaku : MonoBehaviour
{
    const int ShapeLine = 0;
    const int ShapeL = 1;
    const int ShapeT = 2;
    const int ShapeU = 3;
    const int ShapePlus = 4;
    const int ShapeH = 5;
    const int ShapeSmallS = 6;
    const int ShapeSmallZ = 7;
    const int ShapeLargeS = 8;
    const int ShapeLargeZ = 9;
    const int Shape2 = 10;
    const int Shape3 = 11;
    const int Shape4 = 12;
    const int Shape5 = 13;
    const int Shape6 = 14;
    const int Shape7 = 15;

    const int Width = 6;
    const int Height = 6;

    const int OutOfBounds = -1;
    const int Empty = 0;

    enum Direction { Up, Right, Down, Left };

    public KMBombInfo Bomb;
    public KMSelectable Module;
    public GameObject Squares;
    public GameObject ActiveColor;
    public Material[] Materials;

    //private int _moduleId;
    //private static int _moduleIdCounter = 1;
    private bool _isSolved;
    private int[] _puzzle = new int[36];
    private int[] _grid = new int[36];
    private ShapeType[] _shapeTypes = new ShapeType[]
    {
        new ShapeType() { Shape = ShapeLine, Name = "Line", MinSize = 2, HintChars = "ABAB", MaxCount = 2 },
        new ShapeType() { Shape = ShapeL, Name = "L", MinSize = 3, HintChars = "CDEF", MaxCount = 2 },
        new ShapeType() { Shape = ShapeT, Name = "T", MinSize = 4, HintChars = "GHIJ", MaxCount = 2 },
        new ShapeType() { Shape = ShapeU, Name = "U", MinSize = 5, HintChars = "KLMN", MaxCount = 2 },
        new ShapeType() { Shape = ShapePlus, Name = "Plus", MinSize = 5, HintChars = "OOOO", MaxCount = 1 },
        new ShapeType() { Shape = ShapeH, Name = "H", MinSize = 7, HintChars = "QRQR", MaxCount = 1 },
        new ShapeType() { Shape = ShapeSmallS, Name = "Small S", MinSize = 4, HintChars = "STST", MaxCount = 1 },
        new ShapeType() { Shape = ShapeSmallZ, Name = "Small Z", MinSize = 4, HintChars = "UVUV", MaxCount = 1 },
        new ShapeType() { Shape = ShapeLargeS, Name = "Large S", MinSize = 5, HintChars = "WXWX", MaxCount = 1 },
        new ShapeType() { Shape = ShapeLargeZ, Name = "Large Z", MinSize = 5, HintChars = "YZYZ", MaxCount = 1 },
        new ShapeType() { Shape = Shape2, IsNumber = true, Name = "2", MinSize = 2, HintChars = "2222", MaxCount = 3 },
        new ShapeType() { Shape = Shape3, IsNumber = true, Name = "3", MinSize = 3, HintChars = "3333", MaxCount = 3 },
        new ShapeType() { Shape = Shape4, IsNumber = true, Name = "4", MinSize = 4, HintChars = "4444", MaxCount = 2 },
        new ShapeType() { Shape = Shape5, IsNumber = true, Name = "5", MinSize = 5, HintChars = "5555", MaxCount = 2 },
        new ShapeType() { Shape = Shape6, IsNumber = true, Name = "6", MinSize = 6, HintChars = "6666", MaxCount = 1 },
        new ShapeType() { Shape = Shape7, IsNumber = true, Name = "7", MinSize = 7, HintChars = "7777", MaxCount = 1 },
    };
    private KMSelectable[] _buttons = new KMSelectable[36];
    private TextMesh[] _hints = new TextMesh[36];
    private List<Shape> _shapes = new List<Shape>();
    private List<int> _colors;
    private int _activeShape;
    private string _precedence = "GWEKTYAIOUSDMQHJRLBXCVZNF";

    void Start()
    {
        //_moduleId = _moduleIdCounter++;

        for (int i = 0; i < 36; i++)
        {
            var j = i;
            _buttons[i] = Squares.transform.Find(i.ToString()).GetComponent<KMSelectable>();
            _buttons[i].OnInteract += delegate () { PressButton(j); return false; };
            _hints[i] = Squares.transform.Find(i.ToString()).transform.Find("Hint (" + i.ToString() + ")").GetComponent<TextMesh>();
            _hints[i].text = "";
        }

        // Random colors
        _colors = (new List<int>() { 1, 2, 3, 4, 5, 6, 7, 8, 9 }).Shuffle();
        _colors.Insert(0, 0);

        GeneratePuzzle();
        string log = "";
        for (var i = 0; i < _puzzle.Length; i++)
            log += _puzzle[i].ToString() + (i % Width == Width - 1 ? "\n" : "");
        Debug.Log(log);
        Refresh();
    }

    private void PressButton(int i)
    {
        // Pressing a hint
        foreach (var shape in _shapes)
        {
            if (shape.HintNode == i)
            {
                // When it's already the active shape
                if (shape.Number == _activeShape && !shape.ShapeType.IsNumber)
                {
                    shape.CurrentHintCorrect = !shape.CurrentHintCorrect;
                    _hints[shape.HintNode].text = shape.CurrentHintCorrect ? shape.HintChar.ToString() : shape.FakeHintChar.ToString();
                }
                else
                {
                    _activeShape = shape.Number;
                    ActiveColor.GetComponent<Renderer>().material = Materials[_colors[_grid[i]]];
                }
                return;
            }
        }

        // Pressing another square
        if (_activeShape == 0) return;
        _grid[i] = _activeShape;

        Refresh();
        _isSolved = CheckIfSolved();
        if (_isSolved)
        {
            Debug.Log("Solved!");
        }

    }

    private void GeneratePuzzle()
    {
        // Try to generate puzzle
        var puzzleSuccess = false;
        var tryPuzzle = 0;
        while (tryPuzzle < 50)
        {
            tryPuzzle++;

            _shapes = new List<Shape>();
            _puzzle = new int[36];
            for (var i = 0; i < _shapeTypes.Length; i++) _shapeTypes[i].Count = 0;

            // Add some shapes. Try a bunch of times, doesn't really matter how many succeed, we'll check some conditions afterwards.
            var tryShape = 0;
            while (tryShape < 10)
            {
                tryShape++;

                ShapeType shapeType;
                do shapeType = _shapeTypes[Rnd.Range(0, ShapeLargeZ + 1)];
                while (shapeType.Count == shapeType.MaxCount);
                var success = false;

                var tryShapeType = 0;
                while (tryShapeType < 10)
                {
                    tryShapeType++;
                    success = TryToAddShape(shapeType, _shapes.Count + 1);
                    if (success) break;
                }

                DevLog((success ? "Succeeded" : "Failed") + " to add a " + shapeType.Name + " in " + tryShapeType.ToString() + " tries");

                if (_shapes.Count == 6) break;
            }

            if (_shapes.Count < 4)
            {
                DevLog("Could not fit 4 shapes.");
                continue;
            }

            // Make sure we have some exotic shapes in the mix
            if (_shapeTypes[ShapeH].Count + _shapeTypes[ShapePlus].Count + _shapeTypes[ShapeU].Count == 0)
            {
                DevLog("Only easy shapes.");
                continue;
            }

            // Extend the shapes
            int cursorNode;
            foreach (var shape in _shapes)
            {
                foreach (var extension in shape.Extensions)
                {
                    cursorNode = extension.Node;
                    while (Step(ref cursorNode, extension.Direction))
                    {
                        extension.Node = cursorNode;
                        shape.Nodes.Add(cursorNode);
                    }
                    foreach (var n in shape.Nodes) _puzzle[n] = shape.Number;
                }
            }

            // Identify the empty areas that are left
            var failed = false;
            for (var i = 0; i < _puzzle.Length; i++)
            {
                if (_puzzle[i] == 0)
                {
                    // Empty square found. Let's start a shape and see how big it is
                    var shape = new Shape() { Number = _shapes.Count + 1 };
                    var checkNeighbours = new Stack<int>();
                    checkNeighbours.Push(i);
                    while (checkNeighbours.Count > 0)
                    {
                        var node = checkNeighbours.Pop();
                        _puzzle[node] = shape.Number;
                        shape.Nodes.Add(node);
                        foreach (Direction direction in Enum.GetValues(typeof(Direction)))
                        {
                            var newNode = GetNode(node, direction);
                            if (checkNeighbours.Contains(newNode)) continue;
                            if (shape.Nodes.Contains(newNode)) continue;
                            if (newNode == OutOfBounds) continue;
                            if (_puzzle[newNode] != Empty) continue;
                            checkNeighbours.Push(newNode);
                        }
                    }

                    if (shape.Nodes.Count < 2 || shape.Nodes.Count > 7)
                    {
                        DevLog("Empty areas too small or too big.");
                        failed = true;
                        break;
                    }
                    shape.ShapeType = _shapeTypes[8 + shape.Nodes.Count];
                    _shapes.Add(shape);
                    if (_shapes.Count > 9)
                    {
                        DevLog("Too many shapes");
                        failed = true;
                        break;
                    }
                    shape.ShapeType.Count++;
                    if (shape.ShapeType.Count > shape.ShapeType.MaxCount)
                    {
                        DevLog("Too many shapes of size " + shape.ShapeType.Name);
                        failed = true;
                        break;
                    }
                }
            }
            if (_shapeTypes[Shape2].Count + _shapeTypes[Shape3].Count + _shapeTypes[Shape4].Count + _shapeTypes[Shape5].Count + _shapeTypes[Shape6].Count + _shapeTypes[Shape7].Count == 0)
            {
                DevLog("No number shapes");
                failed = true;
            }

            if (failed) continue;
            puzzleSuccess = true;
            break;
        }

        if (!puzzleSuccess)
        {
            DevLog("Giving up :(");
            return;
        }

        DevLog("Generated puzzle in " + tryPuzzle.ToString() + " tries.");

        // Hints
        var sum = _shapeTypes.Select(shape => shape.IsNumber ? shape.Count * shape.MinSize : 0).Sum();
        sum = (sum - 1) % 4 + 1;
        foreach (var shape in _shapes)
        {
            // Random hint location within shape
            shape.HintNode = shape.Nodes[Rnd.Range(0, shape.Nodes.Count)];
            shape.HintChar = shape.ShapeType.HintChars[(int)shape.Direction];

            // Number shapes only have one hint
            if (shape.ShapeType.IsNumber)
            {
                _hints[shape.HintNode].text = shape.HintChar.ToString();
            }

            // Symbol shapes have two hints that toggle
            else
            {
                var hint = _precedence.IndexOf(shape.HintChar);
                int fakeHint = 0;
                var numbers = Enumerable.Range(0, 25).ToList();

                switch (sum)
                {
                    case 1:
                        if (hint / 5 == 4) fakeHint = hint - 5;
                        else fakeHint = numbers.Where(n => n / 5 > hint / 5 && n != hint + 5).PickRandom();
                        break;
                    case 2:
                        if (hint % 5 == 0) fakeHint = hint + 1;
                        else fakeHint = numbers.Where(n => n % 5 < hint % 5 && n != hint - 1).PickRandom();
                        break;
                    case 3:
                        if (hint / 5 == 0) fakeHint = hint + 5;
                        else fakeHint = numbers.Where(n => n / 5 < hint / 5 && n != hint - 5).PickRandom();
                        break;
                    case 4:
                        if (hint % 5 == 4) fakeHint = hint - 1;
                        else fakeHint = numbers.Where(n => n % 5 > hint % 5 && n != hint + 1).PickRandom();
                        break;
                }
                shape.FakeHintChar = _precedence[fakeHint];
                shape.CurrentHintCorrect = Rnd.Range(0, 2) == 0;
                _hints[shape.HintNode].text = shape.CurrentHintCorrect ? shape.HintChar.ToString() : shape.FakeHintChar.ToString();
            }

            // Color the hint nodes
            _grid[shape.HintNode] = shape.Number;
        }
    }

    private bool TryToAddShape(ShapeType shapeType, int number)
    {
        // Random starting node
        var shape = new Shape()
        {
            ShapeType = shapeType,
            Number = number,
            Direction = RandomDirection()
        };

        int startNode;
        do startNode = Rnd.Range(0, _puzzle.Length);
        while (_puzzle[startNode] != Empty);
        var cursorNode = startNode;
        shape.Nodes.Add(cursorNode);
        var direction = shape.Direction;
        var numNodes = 0;

        // Construct basic shape
        switch (shapeType.Shape)
        {
            case ShapeLine:
                shape.Extensions.Add(new Extension() { Node = cursorNode, Direction = Turn180(direction) });
                if (!Step(ref cursorNode, direction)) return false;
                shape.Nodes.Add(cursorNode);
                shape.Extensions.Add(new Extension() { Node = cursorNode, Direction = direction });
                break;
            case ShapeL:
                shape.Extensions.Add(new Extension() { Node = cursorNode, Direction = direction });
                direction = Turn180(direction);
                if (!Step(ref cursorNode, direction)) return false;
                shape.Nodes.Add(cursorNode);
                direction = TurnLeft(direction);
                if (!Step(ref cursorNode, direction)) return false;
                shape.Nodes.Add(cursorNode);
                shape.Extensions.Add(new Extension() { Node = cursorNode, Direction = direction });
                break;
            case ShapeT:
                shape.Extensions.Add(new Extension() { Node = cursorNode, Direction = Turn180(direction) });
                if (!Step(ref cursorNode, direction)) return false;
                shape.Nodes.Add(cursorNode);
                direction = TurnLeft(direction);
                if (!Step(ref cursorNode, direction)) return false;
                shape.Nodes.Add(cursorNode);
                shape.Extensions.Add(new Extension() { Node = cursorNode, Direction = direction });
                direction = Turn180(direction);
                Step(ref cursorNode, direction);
                if (!Step(ref cursorNode, direction)) return false;
                shape.Nodes.Add(cursorNode);
                shape.Extensions.Add(new Extension() { Node = cursorNode, Direction = direction });
                break;
            case ShapeU:
                shape.Extensions.Add(new Extension() { Node = cursorNode, Direction = direction });
                direction = Turn180(direction);
                if (!Step(ref cursorNode, direction)) return false;
                shape.Nodes.Add(cursorNode);
                direction = TurnLeft(direction);
                var maxNodes = Rnd.Range(3, Width);
                numNodes = 1;
                while (Step(ref cursorNode, direction))
                {
                    shape.Nodes.Add(cursorNode);
                    numNodes++;
                    if (numNodes == maxNodes) break;
                }
                if (numNodes < 3) return false;
                direction = TurnLeft(direction);
                if (!Step(ref cursorNode, direction)) return false;
                shape.Nodes.Add(cursorNode);
                shape.Extensions.Add(new Extension() { Node = cursorNode, Direction = direction });
                break;
            case ShapePlus:
                for (var i = 0; i < 4; i++)
                {
                    if (!Step(ref cursorNode, direction)) return false;
                    shape.Nodes.Add(cursorNode);
                    shape.Extensions.Add(new Extension() { Node = cursorNode, Direction = direction });
                    Step(ref cursorNode, Turn180(direction));
                    direction = TurnRight(direction);
                }
                break;
            case ShapeH:
                direction = TurnRight(direction);
                maxNodes = Rnd.Range(3, 6);
                numNodes = 1;
                while (Step(ref cursorNode, direction))
                {
                    shape.Nodes.Add(cursorNode);
                    numNodes++;
                    if (numNodes == maxNodes) break;
                }
                if (numNodes < 3) return false;
                direction = TurnRight(direction);
                if (!Step(ref cursorNode, direction)) return false;
                shape.Nodes.Add(cursorNode);
                shape.Extensions.Add(new Extension() { Node = cursorNode, Direction = direction });
                direction = Turn180(direction);
                Step(ref cursorNode, direction);
                if (!Step(ref cursorNode, direction)) return false;
                shape.Nodes.Add(cursorNode);
                shape.Extensions.Add(new Extension() { Node = cursorNode, Direction = direction });
                cursorNode = startNode;
                if (!Step(ref cursorNode, direction)) return false;
                shape.Nodes.Add(cursorNode);
                shape.Extensions.Add(new Extension() { Node = cursorNode, Direction = direction });
                direction = Turn180(direction);
                Step(ref cursorNode, direction);
                if (!Step(ref cursorNode, direction)) return false;
                shape.Nodes.Add(cursorNode);
                shape.Extensions.Add(new Extension() { Node = cursorNode, Direction = direction });
                break;
            case ShapeSmallS:
                direction = TurnRight(direction);
                shape.Extensions.Add(new Extension() { Node = cursorNode, Direction = Turn180(direction) });
                if (!Step(ref cursorNode, direction)) return false;
                shape.Nodes.Add(cursorNode);
                direction = TurnLeft(direction);
                if (!Step(ref cursorNode, direction)) return false;
                shape.Nodes.Add(cursorNode);
                direction = TurnRight(direction);
                if (!Step(ref cursorNode, direction)) return false;
                shape.Nodes.Add(cursorNode);
                shape.Extensions.Add(new Extension() { Node = cursorNode, Direction = direction });
                break;
            case ShapeSmallZ:
                direction = TurnRight(direction);
                shape.Extensions.Add(new Extension() { Node = cursorNode, Direction = Turn180(direction) });
                if (!Step(ref cursorNode, direction)) return false;
                shape.Nodes.Add(cursorNode);
                direction = TurnRight(direction);
                if (!Step(ref cursorNode, direction)) return false;
                shape.Nodes.Add(cursorNode);
                direction = TurnLeft(direction);
                if (!Step(ref cursorNode, direction)) return false;
                shape.Nodes.Add(cursorNode);
                shape.Extensions.Add(new Extension() { Node = cursorNode, Direction = direction });
                break;
            case ShapeLargeS:
                direction = TurnRight(direction);
                shape.Extensions.Add(new Extension() { Node = cursorNode, Direction = Turn180(direction) });
                if (!Step(ref cursorNode, direction)) return false;
                shape.Nodes.Add(cursorNode);
                direction = TurnLeft(direction);
                maxNodes = Rnd.Range(3, 6);
                numNodes = 1;
                while (Step(ref cursorNode, direction))
                {
                    shape.Nodes.Add(cursorNode);
                    numNodes++;
                    if (numNodes == maxNodes) break;
                }
                if (numNodes < 3) return false;
                direction = TurnRight(direction);
                if (!Step(ref cursorNode, direction)) return false;
                shape.Nodes.Add(cursorNode);
                shape.Extensions.Add(new Extension() { Node = cursorNode, Direction = direction });
                break;
            case ShapeLargeZ:
                direction = TurnRight(direction);
                shape.Extensions.Add(new Extension() { Node = cursorNode, Direction = Turn180(direction) });
                if (!Step(ref cursorNode, direction)) return false;
                shape.Nodes.Add(cursorNode);
                direction = TurnRight(direction);
                maxNodes = Rnd.Range(3, 6);
                numNodes = 1;
                while (Step(ref cursorNode, direction))
                {
                    shape.Nodes.Add(cursorNode);
                    numNodes++;
                    if (numNodes == maxNodes) break;
                }
                if (numNodes < 3) return false;
                direction = TurnLeft(direction);
                if (!Step(ref cursorNode, direction)) return false;
                shape.Nodes.Add(cursorNode);
                shape.Extensions.Add(new Extension() { Node = cursorNode, Direction = direction });
                break;
        }

        foreach (var node in shape.Nodes) _puzzle[node] = shape.Number;
        _shapes.Add(shape);
        shape.ShapeType.Count++;

        return true;
    }

    private Direction RandomDirection()
    {
        var values = Enum.GetValues(typeof(Direction));
        return (Direction)values.GetValue(Rnd.Range(0, values.Length));
    }

    private Direction TurnRight(Direction direction)
    {
        return (Direction)(((int)direction + 1) % 4);
    }

    private Direction Turn180(Direction direction)
    {
        return (Direction)(((int)direction + 2) % 4);
    }

    private Direction TurnLeft(Direction direction)
    {
        return (Direction)(((int)direction + 3) % 4);
    }

    private int GetNode(int node, Direction direction, int numSteps = 1)
    {
        switch (direction)
        {
            case Direction.Up:
                if (node / Height == 0) return OutOfBounds;
                return node - (Height * numSteps);
            case Direction.Right:
                if (node % Width == Width - 1) return OutOfBounds;
                return node + numSteps;
            case Direction.Down:
                if (node / Height == Height - 1) return OutOfBounds;
                return node + (Height * numSteps);
            case Direction.Left:
                if (node % Width == 0) return OutOfBounds;
                return node - numSteps;
            default:
                return OutOfBounds;
        }
    }

    private bool Step(ref int node, Direction direction)
    {
        var newNode = node;
        switch (direction)
        {
            case Direction.Up:
                if (node / Height == 0) return false;
                newNode = node - Height;
                break;
            case Direction.Right:
                if (node % Width == Width - 1) return false;
                newNode = node + 1;
                break;
            case Direction.Down:
                if (node / Height == Height - 1) return false;
                newNode = node + Height;
                break;
            case Direction.Left:
                if (node % Width == 0) return false;
                newNode = node - 1;
                break;
            default:
                return false;
        }

        if (_puzzle[newNode] != Empty)
            return false;

        node = newNode;
        return true;
    }

    private void Refresh()
    {
        for (var i = 0; i < _grid.Length; i++)
        {
            _buttons[i].GetComponent<Renderer>().material = Materials[_colors[_grid[i]]];
        }
    }


    private bool CheckIfSolved()
    {
        int[] overlay = new int[36];
        int node;
        foreach (var shape in _shapes)
        {
            switch (shape.ShapeType.Shape)
            {
                case ShapeLine:
                    break;
                case ShapeL:
                    break;
                case ShapeT:
                    node = FindStartNode(_grid, shape.Number, Turn180(shape.Direction));
                    if (node == OutOfBounds) return false;
                    break;
                case ShapeU:
                    break;
                case ShapePlus:
                    break;
                case ShapeH:
                    break;
                case ShapeSmallS:
                    break;
                case ShapeSmallZ:
                    break;
                case ShapeLargeS:
                    break;
                case ShapeLargeZ:
                    break;
                case Shape2:
                    break;
                case Shape3:
                    break;
                case Shape4:
                    break;
                case Shape5:
                    break;
                case Shape6:
                    break;
                case Shape7:
                    break;
            }
        }
        return true;
    }

    private int FindStartNode(int[] grid, int number, Direction scanFrom)
    {
        var node = 0;
        var scanNode = 0;
        if (scanFrom == Direction.Right) node = Width - 1;
        else if (scanFrom == Direction.Down) node = Width * Height - 1;
        else if (scanFrom == Direction.Left) node = Width * (Height - 1);

        while (true)
        {
            // Check current node
            if (_grid[node] == number) return node;

            // Step in scan direction
            scanNode = GetNode(node, TurnRight(scanFrom));

            // Go to next line if needed
            if (scanNode == OutOfBounds)
            {
                // CR
                scanNode = GetNode(node, TurnLeft(scanFrom), 5);

                // LF
                scanNode = GetNode(scanNode, Turn180(scanFrom));
            }
        }
    }

    private void DevLog(string message)
    {
        Debug.Log(message);
    }

    class ShapeType
    {
        public int Shape { get; set; }
        public bool IsNumber { get; set; }
        public string Name { get; set; }
        public int MinSize { get; set; }
        public string HintChars { get; set; }
        public int MaxCount { get; set; }
        public int Count { get; set; }
    }

    class Extension
    {
        public int Node { get; set; }
        public Direction Direction { get; set; }
    }

    class Shape
    {
        public ShapeType ShapeType { get; set; }
        public List<int> Nodes { get; set; }
        public int HintNode { get; set; }
        public char HintChar { get; set; }
        public char FakeHintChar { get; set; }
        public bool CurrentHintCorrect { get; set; }
        public Direction Direction { get; set; }
        public int Number { get; set; }

        // List of possible extensions, they can be visited in a later stage to fill the gaps
        public List<Extension> Extensions { get; set; }

        public Shape()
        {
            Nodes = new List<int>();
            Extensions = new List<Extension>();
        }
    }
}

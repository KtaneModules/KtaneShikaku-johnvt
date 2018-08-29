using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using KmHelper;
using Rnd = UnityEngine.Random;

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
    public Material[] Colors;

    //private int _moduleId;
    //private static int _moduleIdCounter = 1;
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
        new ShapeType() { Shape = Shape2, Name = "2", MinSize = 2, HintChars = "2222", MaxCount = 3 },
        new ShapeType() { Shape = Shape3, Name = "3", MinSize = 3, HintChars = "3333", MaxCount = 3 },
        new ShapeType() { Shape = Shape4, Name = "4", MinSize = 4, HintChars = "4444", MaxCount = 2 },
        new ShapeType() { Shape = Shape5, Name = "5", MinSize = 5, HintChars = "5555", MaxCount = 2 },
        new ShapeType() { Shape = Shape6, Name = "6", MinSize = 6, HintChars = "6666", MaxCount = 1 },
        new ShapeType() { Shape = Shape7, Name = "7", MinSize = 7, HintChars = "7777", MaxCount = 1 },
    };
    private KMSelectable[] _buttons = new KMSelectable[36];
    private TextMesh[] _hints = new TextMesh[36];
    private List<Shape> _shapes = new List<Shape>();

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

        GeneratePuzzle();
        Refresh();
    }

    private void PressButton(int i)
    {
    }

    private void GeneratePuzzle()
    {
        // Try to generate puzzle
        var puzzleSuccess = false;
        var tryPuzzle = 0;
        while (tryPuzzle < 20)
        {
            tryPuzzle++;

            // Add some shapes. Try a bunch of times, doesn't really matter how many succeed, we'll check some conditions afterwards.
            _shapes = new List<Shape>();
            _grid = new int[36];
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

                if (_shapes.Count == 5) break;
            }

            if (_shapes.Count < 4)
            {
                DevLog("Could not fit 4 shapes.");
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
                    foreach (var n in shape.Nodes) _grid[n] = shape.Color;
                }
            }

            // Identify the empty areas that are left
            var failed = false;
            for (var i = 0; i < _grid.Length; i++)
            {
                if (_grid[i] == 0)
                {
                    // Empty square found. Let's start a shape and see how big it is
                    var shape = new Shape() { Color = _shapes.Count + 1 };
                    var nodesToCheck = new Queue<int>();
                    nodesToCheck.Enqueue(i);
                    while (nodesToCheck.Count > 0)
                    {
                        var node = nodesToCheck.Dequeue();
                        if (node != OutOfBounds && node != Empty && _grid[node] == 0)
                        {
                            _grid[node] = shape.Color;
                            shape.Nodes.Add(node);
                            foreach (Direction direction in Enum.GetValues(typeof(Direction)))
                                nodesToCheck.Enqueue(GetNode(node, direction));
                        }
                    }

                    if (shape.Nodes.Count < 2 || shape.Nodes.Count > 7)
                    {
                        failed = true;
                        break;
                    }
                    shape.ShapeType =_shapeTypes[8 + shape.Nodes.Count];
                    _shapes.Add(shape);
                    shape.ShapeType.Count++;
                }
            }
            if (failed)
            {
                DevLog("Empty areas too small or too big.");
                continue;
            }

            puzzleSuccess = true;
        }

        if (!puzzleSuccess)
        {
            DevLog("Giving up :(");
            return;
        }

        // Random hints
        foreach (var shape in _shapes)
        {
            shape.HintNode = shape.Nodes[Rnd.Range(0, shape.Nodes.Count)];
            _hints[shape.HintNode].text = shape.ShapeType.HintChars[(int)shape.Direction].ToString();
        }
    }

    private bool TryToAddShape(ShapeType shapeType, int color)
    {
        // Random starting node
        var shape = new Shape()
        {
            ShapeType = shapeType,
            Color = color,
            Direction = RandomDirection()
        };

        int startNode;
        do startNode = Rnd.Range(0, _grid.Length);
        while (_grid[startNode] != Empty);
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

        foreach (var node in shape.Nodes) _grid[node] = shape.Color;
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

    private int GetNode(int node, Direction direction)
    {
        switch (direction)
        {
            case Direction.Up:
                if (node / Width == 0) return OutOfBounds;
                return _grid[node - Width];
            case Direction.Right:
                if (node % Width == Width - 1) return OutOfBounds;
                return _grid[node + 1];
            case Direction.Down:
                if (node / Width == Height - 1) return OutOfBounds;
                return _grid[node + Width];
            case Direction.Left:
                if (node % Width == 0) return OutOfBounds;
                return _grid[node - 1];
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
                if (node / Width == 0) return false;
                newNode = node - Width;
                break;
            case Direction.Right:
                if (node % Width == Width - 1) return false;
                newNode = node + 1;
                break;
            case Direction.Down:
                if (node / Width == Height - 1) return false;
                newNode = node + Width;
                break;
            case Direction.Left:
                if (node % Width == 0) return false;
                newNode = node - 1;
                break;
            default:
                return false;
        }

        if (_grid[newNode] != Empty)
            return false;

        node = newNode;
        return true;
    }

    private void Refresh()
    {
        for (var i = 0; i < _grid.Length; i++)
        {
            _buttons[i].GetComponent<Renderer>().material = Colors[_grid[i]];
        }
        string log = "";
        for (var i = 0; i < _grid.Length; i++)
            log += _grid[i].ToString() + (i % Width == Width - 1 ? "\n" : "");
        Debug.Log(log);
    }

    private void DevLog(string message)
    {
        Debug.Log(message);
    }

    class ShapeType
    {
        public int Shape { get; set; }
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
        public Direction Direction { get; set; }
        public int Color { get; set; }
        // List of possible extensions, they can be visited in a later stage to fill the gaps
        public List<Extension> Extensions { get; set; }

        public Shape()
        {
            Nodes = new List<int>();
            Extensions = new List<Extension>();
        }
    }
}

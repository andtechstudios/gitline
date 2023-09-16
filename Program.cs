using System.Text.RegularExpressions;

// Read commit file
var destPath = args.Length > 0 ? args[0] : null;
var lines = new string[3] { string.Empty, string.Empty, string.Empty, };
if (!string.IsNullOrEmpty(destPath))
{
	var msg = File.ReadAllText(destPath);
	msg = Regex.Replace(msg.TrimEnd(), @"^#.*$", string.Empty, RegexOptions.Multiline);
	var splits = msg.Split("\n");

	switch (splits.Length)
	{
		case 0:
			break;
		case 1:
			lines[0] = splits[0];
			break;
		case 2:
			lines[0] = splits[0];
			lines[1] = splits[1];
			break;
		default:
			lines[0] = splits[0];
			lines[1] = splits[1];
			lines[2] = splits[2];
			break;
	}
}

// Reserve lines
Console.WriteLine();
Console.WriteLine();
Console.WriteLine();
var origin = new Vector2Int(0, Console.CursorTop - 3);
var position0 = new Vector2Int(origin.x, origin.y + 0);
var position1 = new Vector2Int(origin.x, origin.y + 1);
var position2 = new Vector2Int(origin.x, origin.y + 2);
var fields = new Field[]
{
	new Field(lines[0], "> ", position0),
	new Field(lines[1], "> ", position1),
	new Field(lines[2], "> ", position2),
};

// Begin input loop
Console.CancelKeyPress += HandleCancelKey;
void HandleCancelKey(object sender, ConsoleCancelEventArgs args)
{
	if (!string.IsNullOrEmpty(destPath))
	{
		File.WriteAllText(destPath, string.Empty);
	}

	Console.SetCursorPosition(origin.x, Math.Max(0, origin.y + 3));
	Environment.Exit(0);
}

// Initialize screen
Console.SetCursorPosition(origin.x, origin.y);
fields[0].Draw(true);
fields[1].Draw(false);
fields[2].Draw(false);

// Loop
var index = 0;
do
{
	fields[index].Run();
	if (fields[index].Status == 0)
	{
		break;
	}

	fields[index].Draw(false);
	index = Math.Clamp(index + fields[index].Status, 0, fields.Length - 1);
}
while (true);

// Write content
var content = string.Join("\n\n", fields.Select(x => x.Value).Where(x => !string.IsNullOrEmpty(x)));
if (string.IsNullOrEmpty(destPath))
{
	Console.WriteLine(content);
}
else
{
	File.WriteAllText(destPath, content);
}
RestoreConsole();

#region helper methods
void RestoreConsole()
{
	Console.SetCursorPosition(origin.x, origin.y);
}
#endregion

#region types
struct Vector2Int
{
	public int x;
	public int y;

	public Vector2Int(int x, int y)
	{
		this.x = x;
		this.y = y;
	}
}

class StringEditor
{
	public int Position { get; set; }
	public int Length => buffer.Count;
	private List<char> buffer = new List<char>();

	public StringEditor(string value)
	{
		buffer = new List<char>(value);
		Position = buffer.Count;
	}

	public void Set(char value) => buffer.Insert(Position, value);
	public void Translate(int offset) => Move(Position + offset);
	public void Move(int index) => Position = Clamp(index);
	public void MoveWordLeft() => Move(FindPreviousBoundary(Position));
	public void MoveWordRight() => Move(FindNextBoundary(Position));
	public void DeleteLeft()
	{
		if (Length > 0)
		{
			if (Position > 0)
			{
				var index = Clamp(Position - 1);
				buffer.RemoveAt(index);
				Position = index;
			}
		}
	}
	public void DeleteRight()
	{
		if (Length > 0)
		{
			if (Position < Length)
			{
				buffer.RemoveAt(Position);
			}
		}
	}
	public void DeleteWordLeft()
	{
		var i = Position;
		var j = FindPreviousBoundary(Position);

		var count = i - j;
		while (count-- > 0)
		{
			buffer.RemoveAt(j);
		}
		Position = j;
	}
	public void DeleteWordRight()
	{
		var i = Position;
		var j = FindNextBoundary(Position);

		var count = j - i;
		while (count-- > 0)
		{
			buffer.RemoveAt(i);
		}
		Position = i;
	}

	int Clamp(int index) => Math.Clamp(index, 0, Length);

	int FindPreviousBoundary(int x)
	{
		x = x - 2;
		while (x > 0)
		{
			if (string.IsNullOrWhiteSpace(buffer[x].ToString()) && !string.IsNullOrWhiteSpace(buffer[x + 1].ToString()))
			{
				return x + 1;
			}

			x--;
		}

		return 0;
	}
	int FindNextBoundary(int x)
	{
		while (x + 1 < Length)
		{
			if (string.IsNullOrWhiteSpace(buffer[x].ToString()) && !string.IsNullOrWhiteSpace(buffer[x + 1].ToString()))
			{
				return x + 1;
			}

			x++;
		}

		return Length;
	}

	public override string ToString() => string.Join(string.Empty, buffer);
}

class Field
{
	public string Value => editor.ToString();
	public int Status { get; private set; }
	private StringEditor editor;
	private Printer printer;

	public Field(string value, string bullet, Vector2Int position)
	{
		editor = new StringEditor(value);
		printer = new Printer(position, bullet);
	}

	public void Draw(bool isHighlighted)
	{
		if (isHighlighted)
		{
			printer.Draw(editor.Position, editor.ToString(), true);
		}
		else
		{
			Console.ForegroundColor = ConsoleColor.DarkGray;
			printer.Draw(editor.Position, editor.ToString(), false);
		}
		Console.ResetColor();
	}

	public void Run()
	{
		while (true)
		{
			Draw(true);

			var keyInfo = Console.ReadKey(intercept: true);
			if (keyInfo.Key == ConsoleKey.Enter)
			{
				Status = 0;
				break;
			}

			if (keyInfo.Key == ConsoleKey.Tab)
			{
				if (keyInfo.Modifiers == ConsoleModifiers.Shift)
				{
					Status = -1;
				}
				else
				{
					Status = 1;
				}
				break;
			}
			else if (keyInfo.Key == ConsoleKey.Backspace)
			{
				if (keyInfo.Modifiers == ConsoleModifiers.Control)
				{
					editor.DeleteWordLeft();
				}
				else
				{
					editor.DeleteLeft();
				}
			}
			else if (keyInfo.Key == ConsoleKey.Delete)
			{
				if (keyInfo.Modifiers == ConsoleModifiers.Control)
				{
					editor.DeleteWordRight();
				}
				else
				{
					editor.DeleteRight();
				}
			}
			else if (keyInfo.Key == ConsoleKey.LeftArrow)
			{
				if (keyInfo.Modifiers == ConsoleModifiers.Control)
				{
					editor.MoveWordLeft();
				}
				else
				{
					editor.Translate(-1);
				}
			}
			else if (keyInfo.Key == ConsoleKey.RightArrow)
			{
				if (keyInfo.Modifiers == ConsoleModifiers.Control)
				{
					editor.MoveWordRight();
				}
				else
				{
					editor.Translate(1);
				}
			}
			else if (keyInfo.Key == ConsoleKey.UpArrow)
			{
				Status = -1;
				break;
			}
			else if (keyInfo.Key == ConsoleKey.DownArrow)
			{
				Status = 1;
				break;
			}
			else if (keyInfo.Key == ConsoleKey.Home)
			{
				editor.Move(0);
			}
			else if (keyInfo.Key == ConsoleKey.End)
			{
				editor.Move(int.MaxValue);
			}
			else if (!char.IsControl(keyInfo.KeyChar))
			{
				editor.Set(keyInfo.KeyChar);
				editor.Translate(1);
			}
		}
	}
}

class Printer
{
	private Vector2Int origin;
	private string bullet;

	public Printer(Vector2Int origin, string bullet)
	{
		this.origin = origin;
		this.bullet = bullet;
	}

	public void Draw(int cursor, string text, bool isHighlighted)
	{
		var indent = bullet.Length;
		text = text.PadRight(Console.WindowWidth - indent);
		if (isHighlighted)
		{
			text = $"\x1b[40m{text}\x1b[0m";
		}

		Console.SetCursorPosition(origin.x, origin.y);
		Console.Write(bullet + text);
		Console.SetCursorPosition(origin.x + indent + cursor, origin.y);
	}
}
#endregion
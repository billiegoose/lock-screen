#region Usage notice
/*
 * Screensaver.cs
 * 
 * (c) Rei Miyasaka 2006
 * rei@thefraser.com
 * 
 * Last updated 2006.05.16
 * 
 * You may use this code for any purpose, in part or in whole, on two conditions:
 * 1. I cannot be held legally responsible for any damage or problems caused by this code.
 * 2. If you make something cool using this code, give me a shout and tell me about it.
 *
 */
#endregion

using System;
using System.Windows.Forms;
using System.Drawing;
using System.Collections;
using System.Text;
using System.Runtime.InteropServices;

namespace Screensavers
{
	/// <summary>
	/// Provides initialization, timing and windowing facilities for screensavers.
	/// </summary>
	public abstract class Screensaver
	{
		/// <summary>
		/// Creates a new <see cref="Screensaver"/> with the given fullscreen mode.
		/// </summary>
		/// <param name="fullscreenMode">A value indicating the fullscreen windowing mode.</param>
		protected Screensaver(FullscreenMode fullscreenMode)
		{
			this.fullscreenMode = fullscreenMode;
			Application.EnableVisualStyles();
			Application.SetCompatibleTextRenderingDefault(false);

			Framerate = 30;

			framerateTimer.Elapsed += new System.Timers.ElapsedEventHandler(framerateTimer_Elapsed);
			framerateTimer.Start();
		}

		/// <summary>
		/// Creates a new <see cref="Screensaver"/> that runs one window per screen.
		/// </summary>
		protected Screensaver()
			: this(FullscreenMode.MultipleWindows)
		{
		}

		void  framerateTimer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
		{
			achievedFramerate = updatesThisSec;
			updatesThisSec = 0;
			if (OneSecondTick != null)
				OneSecondTick(this, new EventArgs());
		}

		/// <summary>
		/// Occurs before the screensaver windows close.
		/// </summary>
		public event EventHandler Exit;

		#region Multimedia Timer

		[DllImport("winmm.dll")]
		static extern int timeSetEvent(int delay, int resolution, TimeCallback callback, int user, int mode);

		[DllImport("winmm.dll")]
		static extern int timeKillEvent(int id);

		[DllImport("winmm.dll")]
		static extern int timeGetTime();

        [DllImport("user32.dll")]
        public static extern bool LockWorkStation();

        delegate void TimeCallback(uint id, uint msg, IntPtr user, IntPtr param1, IntPtr param2);

		TimeCallback timerCallback;

		int timerId;

		void StartUpdating()
		{
			timerCallback = new TimeCallback(TimerCallback);
			//TIME_KILL_SYNCHRONOUS = 0x0100
			//TIME_PERIODIC = 0x0001
			timerId = timeSetEvent((int)(1000/(double)framerate), 0, timerCallback, 0, 0x0101);

			while (timerCallback != null)
			{
				updateEvent.WaitOne();
				DoUpdate();
				Application.DoEvents();
				updateEvent.Reset();
			}

		}

		void StopUpdating()
		{
			timerCallback = null;
			timeKillEvent(timerId);
			updateEvent.WaitOne();
		}

		System.Threading.ManualResetEvent updateEvent = new System.Threading.ManualResetEvent(false);

		void TimerCallback(uint id, uint msg, IntPtr user, IntPtr param1, IntPtr param2)
		{
			updateEvent.Set();
		}

		System.Timers.Timer framerateTimer = new System.Timers.Timer(1000);

		/// <summary>
		/// Occurs once each second on a thread separate from the window thread.
		/// </summary>
		public event EventHandler OneSecondTick;

		int framerate;

		/// <summary>
		/// Gets or sets the target framerate.
		/// </summary>
		public int Framerate
		{
			get { return framerate; }
			set
			{
				if (value < 0)
					throw new ArgumentOutOfRangeException();
				if (timerCallback != null)
				{
					StopUpdating();
					framerate = value;
					StartUpdating();
				}
				else
					framerate = value;
			}
		}

		#endregion

		[StructLayout(LayoutKind.Sequential)]
		struct RECT
		{
			public int left, top, right, bottom;
		}

		[DllImport("user32.dll")]
		static extern bool GetClientRect(IntPtr handle, out RECT rect);
		[DllImport("user32.dll")]
		static extern bool IsWindowVisible(IntPtr handle);

		static Rectangle GetClientRect(IntPtr handle)
		{
			RECT rect;
			GetClientRect(handle, out rect);
			return Rectangle.FromLTRB(rect.left, rect.top, rect.right, rect.bottom);
		}

		/// <summary>
		/// Occurs when the screensaver should process its logic and render.
		/// </summary>
		public event EventHandler Update;

		event EventHandler PreUpdate;
		event EventHandler PostUpdate;

		int achievedFramerate;
		int updatesThisSec;

		/// <summary>
		/// Actual framerate achieved. This value is updated once each second.
		/// </summary>
		public int AchievedFramerate
		{
			get { return achievedFramerate; }
		}

		void DoUpdate()
		{
			if (screensaverMode == ScreensaverMode.Preview && !IsWindowVisible(windowHandle))
			{
				StopUpdating();
				if (Exit != null)
					Exit(this, new EventArgs());
				previewShutdownEvent.Set();
				return;
			}

			if (PreUpdate != null)
				PreUpdate(this, new EventArgs());
			if (Update != null)
				Update(this, new EventArgs());
			if (PostUpdate != null)
				PostUpdate(this, new EventArgs());
			updatesThisSec++;
		}

		ScreensaverMode ProcessCommandLine()
		{
			string[] args = Environment.GetCommandLineArgs();

			if (args.Length == 1 && IsScr)
				return ScreensaverMode.Settings;

			if (args.Length < 2)
				throw new FormatException();

			if (args[1].ToLower().StartsWith("/c"))
			{
				return ScreensaverMode.Settings;
			}

			switch (args[1].ToLower())
			{
				case "w":
					return ScreensaverMode.Windowed;
				case "/s":
					return ScreensaverMode.Normal;
				case "/p":
					if (args.Length < 3)
					{
						throw new FormatException();
					}
					try
					{
						windowHandle = (IntPtr)uint.Parse(args[2]);
						return ScreensaverMode.Preview;
					}
					catch (FormatException)
					{
						throw new FormatException();
					}
				default:
					throw new FormatException();
			}
		}

		bool IsScr
		{
			get
			{
				return System.IO.Path.GetExtension(System.Reflection.Assembly.GetExecutingAssembly().CodeBase).
					Equals(".scr", StringComparison.InvariantCultureIgnoreCase);
			}
		}

		/// <summary>
		/// Start the screensaver in windowed mode if the file extension is not scr, unless a mode is specified in the command line.
		/// Otherwise, if the file extension is scr, start the screensaver in config mode.
		/// </summary>
		public void Run()
		{
			Run(ScreensaverMode.Windowed);
		}

		/// <summary>
		/// Start the screensaver in the specified mode unless one is specified in the command line.
		/// </summary>
		/// <param name="mode">The mode in which to run the screensaver. This value cannot be <see cref="ScreensaverMode.Preview"/>.</param>
		public void Run(ScreensaverMode mode)
		{
			if (mode == ScreensaverMode.Preview && windowHandle == IntPtr.Zero)
				throw new ArgumentException("Cannot explicity run in preview mode", "mode");

			if (isEnded)
				throw new Exception("This screensaver has already finished running");

			try
			{
				this.screensaverMode = ProcessCommandLine();
			}
			catch (FormatException)
			{
				this.screensaverMode = mode;
			}

			try
			{
				switch (screensaverMode)
				{
					case ScreensaverMode.Windowed:
						RunWindowed();
						break;
					case ScreensaverMode.Settings:
						ShowSettingsDialog();
						break;
					case ScreensaverMode.Normal:
						if (!closeOnMouseMoveOverride)
							closeOnMouseMove = true;
						if (!closeOnClickOverride)
							closeOnClick = true;
						if (!closeOnKeyboardInputOverride)
							closeOnKeyboardInput = true;

						RunNormal();
						break;
					case ScreensaverMode.Preview:
						RunPreview();
						break;
				}
			}
			finally
			{
				isEnded = true;
			}

		}

		FullscreenMode fullscreenMode = FullscreenMode.SingleWindow;
		ScreensaverMode screensaverMode;

		/// <summary>
		/// Gets the current running mode of the screensaver.
		/// </summary>
		public ScreensaverMode Mode
		{
			get { return screensaverMode; }
		}

		IntPtr windowHandle = IntPtr.Zero;

		/// <summary>
		/// Occurs after the windows are created, before the screensaver runs.
		/// </summary>
		public event EventHandler Initialize;

		bool closeOnMouseMove;
		bool closeOnMouseMoveOverride;

		/// <summary>
		/// Gets or sets a value indicating whether or not the screensaver should close when the user moves the mouse.
		/// </summary>
		/// <remarks>This value is <c>true</c> by default in all modes except <see cref="ScreensaverMode.Windowed"/>.</remarks>
		public bool CloseOnMouseMove
		{
			get { return closeOnMouseMove; }
			set { closeOnMouseMove = value; closeOnMouseMoveOverride = true; }
		}

		bool closeOnClick;
		bool closeOnClickOverride;

		/// <summary>
		/// Gets or sets a value indicating whether or not the screensaver should close when the user clicks the mouse.
		/// </summary>
		/// <remarks>This value is <c>true</c> by default in all modes except <see cref="ScreensaverMode.Windowed"/>.</remarks>
		public bool CloseOnClick
		{
			get { return closeOnClick; }
			set { closeOnClick = value; closeOnClickOverride = true; }
		}

		bool closeOnKeyboardInput;
		bool closeOnKeyboardInputOverride;

		/// <summary>
		/// Gets or sets a value indicating whether or not the screensaver should close when the user presses a key.
		/// </summary>
		/// <remarks>This value is <c>true</c> by default in all modes except <see cref="ScreensaverMode.Windowed"/>.</remarks>
		public bool CloseOnKeyboardInput
		{
			get { return closeOnKeyboardInput; }
			set { closeOnKeyboardInput = value; closeOnKeyboardInputOverride = true; }
		}

		WindowCollection windows;

		/// <summary>
		/// Gets a collection of all of the running screensaver windows.
		/// </summary>
		public WindowCollection Windows
		{
			get { return windows; }
		}

		Window window0;

		/// <summary>
		/// Gets the primary screensaver window.
		/// </summary>
		public Window Window0
		{
			get
			{
				if(window0 != null)
					return window0;

				if (windows == null || windows.Count == 0)
					return null;

				window0 = windows[0];

				return window0;
			}
		}

		Graphics graphics0;

		/// <summary>
		/// Gets the GDI graphics object for the primary window.
		/// </summary>
		public Graphics Graphics0
		{
			get
			{
				if (graphics0 != null)
					return graphics0;

				if (Window0 == null)
					return null;

				graphics0 = Window0.Graphics;
				return graphics0;
			}
		}

		string settingsText;

		/// <summary>
		/// Gets or sets text to be displayed in the default settings message box.
		/// </summary>
		public string SettingsText
		{
			get { return settingsText; }
			set { settingsText = value; }
		}

		/// <summary>
		/// Shows the settings dialog, or, by default, shows a message box indicating the assembly name, version and copyright information.
		/// </summary>
		protected virtual void ShowSettingsDialog()
		{
			System.IO.StringWriter sw = new System.IO.StringWriter();
			System.Reflection.AssemblyName name = System.Reflection.Assembly.GetExecutingAssembly().GetName();
			sw.WriteLine(name.Name);
			sw.WriteLine("Version " + name.Version);

			object[] attribs =
				System.Reflection.Assembly.GetExecutingAssembly().
				GetCustomAttributes(typeof(System.Reflection.AssemblyDescriptionAttribute), false);
			if (attribs != null && attribs.Length != 0)
			{
				System.Reflection.AssemblyDescriptionAttribute desc = attribs[0] as System.Reflection.AssemblyDescriptionAttribute;
				if (desc.Description != string.Empty)
				{
					sw.WriteLine(desc.Description);
				}
			}

			attribs =
				System.Reflection.Assembly.GetExecutingAssembly().
				GetCustomAttributes(typeof(System.Reflection.AssemblyCopyrightAttribute), false);
			if (attribs != null && attribs.Length != 0)
			{
				System.Reflection.AssemblyCopyrightAttribute copyright = attribs[0] as System.Reflection.AssemblyCopyrightAttribute;
				if (copyright.Copyright != string.Empty)
				{
					sw.WriteLine();
					sw.WriteLine(copyright.Copyright);
				}
			}

			if (settingsText != null && settingsText != string.Empty)
			{
				sw.WriteLine();
				sw.WriteLine(SettingsText);
			}

			MessageBox.Show(sw.ToString(), "PixieSaver", MessageBoxButtons.OK);
		}

		System.Threading.AutoResetEvent previewShutdownEvent = new System.Threading.AutoResetEvent(false);

		private void RunPreview()
		{
#if DEBUG
			System.Diagnostics.Debugger.Launch();
#endif
			windows = new WindowCollection(new Window[] { new Window(this, windowHandle) });
			InitializeAndStart();
			previewShutdownEvent.WaitOne();
		}

		private void RunNormal()
		{
			Cursor.Hide();
			switch (fullscreenMode)
			{
				case FullscreenMode.SingleWindow:
					RunNormalSingleWindow();
					break;
				case FullscreenMode.MultipleWindows:
					RunNormalMultipleWindows();
					break;
			}
		}

		private void RunNormalMultipleWindows()
		{
			//List<Window> windows = new List<Window>();
			ArrayList windows = new ArrayList();

			Form primary = new Form();
			primary.StartPosition = FormStartPosition.Manual;
			primary.Location = Screen.PrimaryScreen.Bounds.Location;
			primary.Size = Screen.PrimaryScreen.Bounds.Size;
			primary.BackColor = Color.Black;
#if !DEBUG
			primary.TopMost = true;
#endif
			primary.FormBorderStyle = FormBorderStyle.None;
			primary.Text = System.Reflection.Assembly.GetExecutingAssembly().GetName().Name;
            primary.Icon = new Icon("Resources/blank_screen.ico");

			foreach (Screen screen in Screen.AllScreens)
			{
				if (screen == Screen.PrimaryScreen)
					continue;

				Form form = new Form();
				form.Owner = primary;
				form.BackColor = Color.Black;
#if !DEBUG
				form.TopMost = true;
#endif
				form.StartPosition = FormStartPosition.Manual;
				form.Location = screen.Bounds.Location;
				form.Size = screen.Bounds.Size;
				form.FormBorderStyle = FormBorderStyle.None;
				form.Text = primary.Text;
                form.Icon = new Icon("Resources/blank_screen.ico");

                windows.Add(new Window(this, form));
			}

			windows.Insert(0, new Window(this, primary));

			primary.Load += delegate(object sender, EventArgs e)
			{
				foreach (Window window in this.windows)
				{
					if (window.Form.Owner == null)
						continue;
					window.Form.Show();
				}
			};

			this.windows = new WindowCollection(windows.ToArray(typeof(Window)) as Window[]);

			primary.Show();
			InitializeAndStart();
		}

		private void RunNormalSingleWindow()
		{
			Form form = new Form();
			Rectangle rect = GetVirtualScreenRect();
			form.Location = rect.Location;
			form.Size = rect.Size;
			form.BackColor = Color.Black;
#if !DEBUG
			form.TopMost = true;
#endif
			form.FormBorderStyle = FormBorderStyle.None;
			form.StartPosition = FormStartPosition.Manual;
			form.Text = System.Reflection.Assembly.GetExecutingAssembly().GetName().Name;

			windows = new WindowCollection(new Window[] { new Window(this, form) });

			form.Show();
			InitializeAndStart();
		}

		static Rectangle GetVirtualScreenRect()
		{
			Screen[] screens = Screen.AllScreens;
			Rectangle rect = Rectangle.Empty;
			foreach (Screen screen in Screen.AllScreens)
				rect = Rectangle.Union(rect, screen.Bounds);
			return rect;
		}

		private void RunWindowed()
		{

			Form form = new Form();
			form.FormBorderStyle = FormBorderStyle.FixedSingle;
			form.Text = System.Reflection.Assembly.GetExecutingAssembly().GetName().Name;
			form.StartPosition = FormStartPosition.CenterScreen;
			form.BackColor = Color.Black;
#if !DEBUG
			form.TopMost = true;
#endif
			form.MaximizeBox = false;
			form.ClientSize = new Size((int)(Screen.PrimaryScreen.WorkingArea.Width * 0.9), (int)(Screen.PrimaryScreen.WorkingArea.Height * 0.9));

			windows = new WindowCollection(new Window[] { new Window(this, form) });

			form.Show();
			InitializeAndStart();
		}

		void InitializeAndStart()
		{
			if (Initialize != null)
				Initialize(this, new EventArgs());

			if (Window0 != null && Window0.Form != null)
				Window0.Form.FormClosing += new FormClosingEventHandler(Form_FormClosing);

			StartUpdating();
		}

		void Form_FormClosing(object sender, FormClosingEventArgs e)
		{
			StopUpdating();
            LockWorkStation();
            if (Exit != null)
				Exit(this, new EventArgs());            
			e.Cancel = false;
		}

		#region IDisposable Members

		bool isEnded = false;

		#endregion

		void OnMouseMove()
		{
			//if (closeOnMouseMove)
			//{
			//	if (Window0.Form != null)
			//		Window0.Form.Close();
			//	else
			//		Application.Exit();
			//}
		}

		void OnKeyboardInput()
		{
			if (closeOnMouseMove)
			{
				if (Window0.Form != null)
					Window0.Form.Close();
				else
					Application.Exit();
			}
		}

		void OnMouseClick()
		{
            if (closeOnMouseMove)
			{
				if (Window0.Form != null)
					Window0.Form.Close();
				else
                {                    
                    Application.Exit();
                }					
			}
		}

		/// <summary>
		/// Represents a screensaver window.
		/// </summary>
		public class Window
		{
			internal Window(Screensaver screensaver, Form form)
			{
				this.screensaver = screensaver;
				this.form = form;
				this.size = form.ClientSize;
				this.graphics = form.CreateGraphics();
				this.handle = form.Handle;

				form.MouseMove += new MouseEventHandler(form_MouseMove);
				form.MouseClick += new MouseEventHandler(form_MouseClick);
				form.MouseDoubleClick += new MouseEventHandler(form_MouseDoubleClick);
				form.MouseDown += new MouseEventHandler(form_MouseDown);
				form.MouseUp += new MouseEventHandler(form_MouseUp);
				form.MouseWheel += new MouseEventHandler(form_MouseWheel);

				form.KeyDown += new KeyEventHandler(form_KeyDown);
				form.KeyUp += new KeyEventHandler(form_KeyUp);
				form.KeyPress += new KeyPressEventHandler(form_KeyPress);

                //form.BackColor = Color.Lime;
                //form.TransparencyKey = Color.Lime;
                form.Opacity = 0.01;

				this.screensaver.PreUpdate += new EventHandler(screensaver_PreUpdate);
				this.screensaver.PostUpdate += new EventHandler(screensaver_PostUpdate);
			}

			internal Window(Screensaver screensaver, IntPtr handle)
			{
				this.screensaver = screensaver;
				this.handle = handle;
				this.graphics = Graphics.FromHwnd(handle);
				this.size = GetClientRect(handle).Size;

				this.screensaver.PreUpdate += new EventHandler(screensaver_PreUpdate);
				this.screensaver.PostUpdate += new EventHandler(screensaver_PostUpdate);
			}

			bool doubleBuffer = false;
			bool doubleBufferSet = false;

			/// <summary>
			/// Gets or sets a value indicating whether or not the Graphics object should be double buffered.
			/// Set to <c>false</c> if the Graphics object will not be used.
			/// </summary>
			public bool DoubleBuffer
			{
				get
				{
					if (!doubleBufferSet)
						DoubleBuffer = true;
					return doubleBuffer;
				}
				set
				{
					doubleBufferSet = true;
					if (doubleBuffer != value)
					{
						doubleBuffer = value;
						if (doubleBuffer)
							SetDoubleBuffer();
						else
							UnsetDoubleBuffer();
					}
					else
						doubleBuffer = value;
				}
			}

			private void SetDoubleBuffer()
			{
				graphicsSwap = graphics;
				BufferedGraphicsManager.Current.MaximumBuffer = this.Size;
				buffer = BufferedGraphicsManager.Current.Allocate(graphicsSwap, new Rectangle(0, 0, Size.Width, Size.Height));
				graphics = buffer.Graphics;
			}

			private void UnsetDoubleBuffer()
			{
				buffer.Dispose();
				graphics = graphicsSwap;
				buffer = null;
				graphicsSwap = null;
			}

			BufferedGraphics buffer;
			Graphics graphicsSwap;

			void screensaver_PreUpdate(object sender, EventArgs e)
			{
			}

			void screensaver_PostUpdate(object sender, EventArgs e)
			{
				if(doubleBuffer)
				{
					buffer.Render(graphicsSwap);
				}
			}

			#region Keyboard and Mouse Events

			void form_KeyPress(object sender, KeyPressEventArgs e)
			{
				if (KeyPress != null)
					KeyPress(this, e);
				screensaver.OnKeyboardInput();
			}

			void form_KeyUp(object sender, KeyEventArgs e)
			{
				if (KeyUp != null)
					KeyUp(this, e);
				screensaver.OnKeyboardInput();
			}

			void form_KeyDown(object sender, KeyEventArgs e)
			{
				if (KeyDown != null)
					KeyDown(this, e);
				screensaver.OnKeyboardInput();
			}

			void form_MouseWheel(object sender, MouseEventArgs e)
			{
				if (MouseWheel != null)
					MouseWheel(this, e);
				screensaver.OnMouseClick();
			}

			void form_MouseUp(object sender, MouseEventArgs e)
			{
				if (MouseUp != null)
					MouseUp(this, e);
				screensaver.OnMouseClick();
			}

			void form_MouseDown(object sender, MouseEventArgs e)
			{
				if (MouseDown!= null)
					MouseDown(this, e);
				screensaver.OnMouseClick();
			}

			void form_MouseDoubleClick(object sender, MouseEventArgs e)
			{
				if (MouseDoubleClick != null)
					MouseDoubleClick(this, e);
				screensaver.OnMouseClick();
			}

			void form_MouseClick(object sender, MouseEventArgs e)
			{
				if (MouseClick != null)
					MouseClick(this, e);
				screensaver.OnMouseClick();
			}

			//Keep track of the initial mouse position since we want to ignore the MouseMove messages that are fired right when the form is created.
			Point mousePosition = Point.Empty;

			void form_MouseMove(object sender, MouseEventArgs e)
			{
				if (MouseMove != null)
					MouseMove(this, e);

				if (mousePosition == Point.Empty)
					mousePosition = e.Location;
				else if (mousePosition != e.Location)
					screensaver.OnMouseMove();
			}

			/// <summary>
			/// Occurs when the mouse is moved over this window.
			/// </summary>
			public event MouseEventHandler MouseMove;
			/// <summary>
			/// Occurs when the mouse is clicked inside this window.
			/// </summary>
			public event MouseEventHandler MouseClick;
			/// <summary>
			/// Occurs when the mouse is double clicked inside this window.
			/// </summary>
			public event MouseEventHandler MouseDoubleClick;
			/// <summary>
			/// Occurs when the mouse wheel is moved inside this window.
			/// </summary>
			public event MouseEventHandler MouseWheel;
			/// <summary>
			/// Occurs when a mouse button goes up inside this window.
			/// </summary>
			public event MouseEventHandler MouseUp;
			/// <summary>
			/// Occurs when a mouse button goes down inside this window.
			/// </summary>
			public event MouseEventHandler MouseDown;

			/// <summary>
			/// Occurs when a key goes down.
			/// </summary>
			public event KeyEventHandler KeyDown;
			/// <summary>
			/// Occurs when a key is released.
			/// </summary>
			public event KeyEventHandler KeyUp;
			/// <summary>
			/// Occurs when a key is pressed.
			/// </summary>
			public event KeyPressEventHandler KeyPress;

			#endregion

			object tag;

			/// <summary>
			/// Gets or sets a tag value.
			/// </summary>
			public object Tag
			{
				get { return tag; }
				set { tag = value; }
			}

			Screensaver screensaver;

			/// <summary>
			/// Gets the <see cref="Screensaver"/> for which this window was created.
			/// </summary>
			public Screensaver Screensaver
			{
				get { return screensaver; }
			}

			Form form;

			/// <summary>
			/// Gets the form encapsulating this window.
			/// This property is <c>null</c> if the screensaver is running in preview mode.
			/// </summary>
			public Form Form
			{
				get { return form; }
			}

			IntPtr handle;

			/// <summary>
			/// Gets the native handle of the window.
			/// </summary>
			public IntPtr Handle
			{
				get { return handle; }
			}

			Size size;

			/// <summary>
			/// Gets the size of the window.
			/// </summary>
			public Size Size
			{
				get { return size; }
			}

			Graphics graphics;

			/// <summary>
			/// Gets the GDI graphics object for this window.
			/// </summary>
			public Graphics Graphics
			{
				get
				{
					//Only set double buffering if the Graphics object is being used.
					if (!doubleBufferSet)
						DoubleBuffer = true;
					return graphics;
				}
			}

			/// <summary>
			/// Gets the screen on which this window resides
			/// </summary>
			public Screen Screen
			{
				get
				{
					return Screen.FromHandle(handle);
				}
			}

			/// <summary>
			/// Gets the display device index to use with this window.
			/// </summary>
			public int DeviceIndex
			{
				get
				{
					Screen thisScreen = Screen;
					for (int i = 0; i < Screen.AllScreens.Length; i++)
						if (Screen.AllScreens[i].Equals(thisScreen))
							return i;
					throw new ApplicationException();
				}
			}
		}

		/// <summary>
		/// Represents a collection of screensaver windows.
		/// </summary>
		public class WindowCollection : IEnumerable
		{
			internal WindowCollection(Window[] windows)
			{
				this.windows = windows;
			}

			Window[] windows;

			/// <summary>
			/// Gets the window at the given index.
			/// </summary>
			/// <param name="index">The zero-based index of the screensaver window.</param>
			/// <returns>The window at the given index.</returns>
			public Window this[int index]
			{
				get { return windows[index]; }
			}

			/// <summary>
			/// Gets the number of screensaver windows available.
			/// </summary>
			public int Count
			{
				get { return windows.Length; }
			}

			#region IEnumerable Members

			System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
			{
				return windows.GetEnumerator();
			}

			#endregion
		}
	}

	/// <summary>
	/// Specifies the types of multiple monitor support to make available.
	/// </summary>
	public enum FullscreenMode
	{
		/// <summary>
		/// Single window covering all monitors.
		/// </summary>
		SingleWindow,
		/// <summary>
		/// Multiple windows, one for each monitor.
		/// </summary>
		MultipleWindows
	}

	/// <summary>
	/// Specifies the mode in which to run the screensaver.
	/// </summary>
	public enum ScreensaverMode
	{
		/// <summary>
		/// Show a the settings dialog.
		/// </summary>
		Settings,
		/// <summary>
		/// Render inside the preview box of the Windows Display Properties.
		/// </summary>
		Preview,
		/// <summary>
		/// Run the screensaver in full screen mode.
		/// </summary>
		Normal,
		/// <summary>
		/// Run the screensaver inside a fixed-sized window.
		/// </summary>
		Windowed
	}
}
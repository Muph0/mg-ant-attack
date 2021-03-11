using Microsoft.Xna.Framework.Input;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace zapoctak_antattack
{
    enum ControlKeys
    {
        Up = Keys.Up,
        Down = Keys.Down,
        Left = Keys.Left,
        Right = Keys.Right,

        Bomb = Keys.Space,
    }

    /// <summary>
    /// Handles basic user input functionality.
    /// </summary>
    class UserInput
    {
        KeyboardState state, stateOld;

        public void Update()
        {
            stateOld = state;
            state = Keyboard.GetState();
        }

        public bool KeyDown(ControlKeys key) { return this.KeyDown((Keys)key); }
        public bool KeyDown(Keys key)
        {
            return state.IsKeyDown(key);
        }

        /// <summary>
        /// Checks for rising edge of a keystroke.
        /// </summary>
        /// <param name="key"></param>
        /// <returns>True if the key was just pressed.</returns>
        public bool KeyPressed(ControlKeys key) { return this.KeyPressed((Keys)key); }
        public bool KeyPressed(Keys key)
        {
            return state.IsKeyDown(key) && stateOld.IsKeyUp(key);
        }

        /// <summary>
        /// Checks for falling edge of a keystroke.
        /// </summary>
        /// <param name="key"></param>
        /// <returns>True if the key was just released.</returns>
        public bool KeyReleased(ControlKeys key) { return this.KeyReleased((Keys)key); }
        public bool KeyReleased(Keys key)
        {
            return state.IsKeyUp(key) && stateOld.IsKeyDown(key);
        }

        
    }
}

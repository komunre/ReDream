using System;
using ReDream.Shared;

namespace TestGame {
	public class TestClass : GameObject {
		public override void Start(ReGame game) {
			game.Host(9999);
		}
		public override void Update(ReGame game) {
			game.ReceiveMessages();
			if (game.Action == "right") {
				ReTools.Log("Moving right!");
				X++;
			}
			game.Action = "";
		}
	}
}
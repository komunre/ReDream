using ReDream.Shared;

namespace TestGame {
	public class TestClass : GameObject {
		public override void Start(ReGame game) {
			Texture = "test.png";
			game.Host(9999);
			X = 1;
			Y = 1;
		}
		public override void Update(ReGame game) {
			base.Update(game);
			game.ReceiveMessages();
			if (game.Action == "right") {
				ReTools.Log("Moving right!");
				X++;
			}
			game.Action = "";
		}
	}
}
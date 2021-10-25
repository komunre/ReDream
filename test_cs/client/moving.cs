using ReDream.Shared;
using ReDream.Client;

namespace ClientCode {
	public class MoveObject : GameObject {
		public override void Update(ReGame game) {
			if (ClientWorker.GetInput() == 69) {
				ReTools.Log("Moving right, huh? Doing!");
				ClientWorker.SendInput("right");
			}
		}
	}
}
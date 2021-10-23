using System;
using ReDream.Shared;

namespace TestGame {
	public class TestClass : GameObject {
		public override void Update(ReGame game) {
			ReTools.Log("HELLO WORLD!");
		}
	}
}
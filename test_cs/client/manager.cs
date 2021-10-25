using ReDream.Client;
using ReDream.Shared;
using System.IO;
using System;

namespace ClientCode {
	public class Manager : GameObject {
		public override void Start(ReGame game) {
			
		}
		public override void Update(ReGame game) {
			ClientTools.Works();
		}
	}
}
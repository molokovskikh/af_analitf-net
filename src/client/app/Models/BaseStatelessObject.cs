using AnalitF.Net.Client.Helpers;

namespace AnalitF.Net.Client.Models
{
	public abstract class BaseStatelessObject : BaseNotify
	{
		public abstract uint Id { get; set; }

		//перегрузка Equals и GetHashCode
		//нужна что бы DataGrid сохранял выделенную позицию после обновления данных
		public override bool Equals(object obj)
		{
			var that = obj as BaseStatelessObject;
			if (that == null)
				return false;

			if (Id == 0 && that.Id == 0)
				return base.Equals(obj);

			return Id == that.Id;
		}

		public override int GetHashCode()
		{
			if (Id == 0)
				return base.GetHashCode();
			return Id.GetHashCode();
		}
	}
}
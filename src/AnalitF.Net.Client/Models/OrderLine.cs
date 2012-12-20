using System;
using System.Collections.Generic;
using System.ComponentModel;
using Common.Tools;
using NHibernate.Cfg;
using Newtonsoft.Json;

namespace AnalitF.Net.Client.Models
{
	public class OrderLine : BaseOffer, INotifyPropertyChanged
	{
		private uint count;

		public OrderLine()
		{
		}

		public OrderLine(Order order, Offer offer, uint count)
			: base(offer)
		{
			Order = order;
			OfferId = offer.Id;
			Count = count;
		}

		public virtual uint Id { get; set; }

		[JsonIgnore]
		public virtual Order Order { get; set; }

		public virtual uint Count
		{
			get { return count; }
			set
			{
				count = value;
				OnPropertyChanged("Count");
				OnPropertyChanged("Sum");
			}
		}

		public virtual OfferComposedId OfferId { get; set; }

		public virtual decimal Sum
		{
			get
			{
				return Count * Cost;
			}
		}

		public virtual List<Message> EditValidate()
		{
			var result = new List<Message>();
			if (Count == 0)
				return result;

			if (Count > 65535) {
				Count = 65535;
			}

			var quantity = SafeConvert.ToUInt32(Quantity);
			if (quantity > 0 && Count > quantity) {
				Count = CalculateAvailableQuantity(Count);
				result.Add(Message.Error(String.Format("Заказ превышает остаток на складе, товар будет заказан в количестве {0}", Count)));
			}

			if (Junk)
				result.Add(Message.Warning("Вы заказали препарат с ограниченным сроком годности\r\nили с повреждением вторичной упаковки."));

			if (Count > 1000)
				result.Add(Message.Warning("Внимание! Вы заказали большое количество препарата."));

			//Заготовка что бы не забыть о проверках
			//if (false) {
			//	warnings.Add("Товар присутствует в замороженных заказах.");
			//}

			//if (false) {
			//	warnings.Add("Превышение среднего заказа!");
			//}

			//if (false) {
			//	warnings.Add("Превышение средней цены!");
			//}
			return result;
		}

		public virtual List<Message> SaveValidate()
		{
			var result = new List<Message>();
			if (Count == 0)
				return result;

			string error = null;
			if (Count % RequestRatio.GetValueOrDefault(1) != 0) {
				error = String.Format("Поставщиком определена кратность по заказываемой позиции.\r\nВведенное значение \"{0}\" не кратно установленному значению \"{1}\"",
					Count,
					RequestRatio);
			}
			else if (MinOrderSum != null && Sum < MinOrderSum) {
				error = String.Format("Сумма заказа \"{0}\" меньше минимальной сумме заказа \"{1}\" по данной позиции!",
					Sum,
					MinOrderSum);
			}

			else if (MinOrderCount != null && Count < MinOrderCount) {
				error = String.Format("'Заказанное количество \"{0}\" меньше минимального количества \"{1}\" по данной позиции!'",
					Count,
					MinOrderCount);
			}

			if (!String.IsNullOrEmpty(error)) {
				Count = CalculateAvailableQuantity(Count);
				result.Add(Message.Error(error));
			}
			//заготовка что бы не забыть
			//проверка матрицы
			//if (false) {
			//	return "Препарат не входит в разрешенную матрицу закупок.\r\nВы действительно хотите заказать его?";
			//}
			return result;
		}

		private uint CalculateAvailableQuantity(uint quantity)
		{
			var topBound = SafeConvert.ToUInt32(Quantity);
			if (topBound == 0)
				topBound = uint.MaxValue;
			topBound = Math.Min(topBound, quantity);
			var bottomBound = Math.Ceiling(MinOrderSum.GetValueOrDefault() / Cost);
			bottomBound = Math.Max(MinOrderCount.GetValueOrDefault(), bottomBound);
			var result = topBound - (topBound % RequestRatio.GetValueOrDefault(1));
			if (result < bottomBound) {
				return 0;
			}
			return result;
		}

		public virtual event PropertyChangedEventHandler PropertyChanged;

		protected virtual void OnPropertyChanged(string propertyName)
		{
			var handler = PropertyChanged;
			if (handler != null)
				handler(this, new PropertyChangedEventArgs(propertyName));
		}
	}
}
using AnalitF.Net.Client.Config.NHibernate;
using Caliburn.Micro;
using System;
using System.Runtime.Serialization;

namespace AnalitF.Net.Client.Models
{
	public class SignTorg12Autosave
	{
		public SignTorg12Autosave()
		{
			CreationDate = DateTime.Now;
		}

		public virtual uint Id { get; set; }
		public virtual DateTime CreationDate { get; set; }
		public virtual string SignerJobTitle { get; set; }
		public virtual bool LikeReciever { get; set; }
		public virtual string AcptSurename { get; set; }
		public virtual string AcptFirstName { get; set; }
		public virtual string AcptPatronimic { get; set; }
		public virtual string AcptJobTitle { get; set; }
		public virtual bool ByAttorney { get; set; }
		public virtual string AtrNum { get; set; }
		public virtual DateTime AtrDate { get; set; }
		public virtual string AtrOrganization { get; set; }
		public virtual string AtrSurename { get; set; }
		public virtual string AtrFirstName { get; set; }
		public virtual string AtrPatronymic { get; set; }
		public virtual string AtrAddInfo { get; set; }
		public virtual string Comment { get; set; }

		[IgnoreDataMember, Ignore]
		public virtual string DisplayName
		{
			get
			{
				if(LikeReciever)
					if(ByAttorney)
						return $"{SignerJobTitle}/Совпд. с получ./По довер./{AtrNum}/{AtrDate.ToString("dd.MM.yyyy")}/"+
							$"{AtrOrganization}/{AtrSurename}/{AtrFirstName}/{AtrPatronymic}/{AtrAddInfo}/{Comment}";
					else
						return $"{SignerJobTitle}/Совпд. с получ./"+
							$"{Comment}";
				else
					if(ByAttorney)
						return $"{SignerJobTitle}/{AcptSurename}/{AcptFirstName}/{AcptPatronimic}/По довер./{AtrNum}/{AtrDate.ToString("dd.MM.yyyy")}/"+
							$"{AtrOrganization}/{AtrSurename}/{AtrFirstName}/{AtrPatronymic}/{AtrAddInfo}/{Comment}";
					else
						return $"{SignerJobTitle}/{AcptSurename}/{AcptFirstName}/{AcptPatronimic}/"+
							$"{Comment}";
			}
		}
	}
}
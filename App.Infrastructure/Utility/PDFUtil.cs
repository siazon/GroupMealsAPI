using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace App.Domain.Common
{
    public interface IPDFUtil
    {
        Document GeneratePdf(List<PDFModel> PDFData);
    }
    public class PDFUtil : IPDFUtil
    {
        List<PDFModel> _PDFData;
        public Document GeneratePdf(List<PDFModel> PDFData)
        {
            _PDFData = PDFData;
            var doc = Document.Create(container =>
             {
                 container.Page(page =>
                 {
                     page.Margin(50);

                     page.Header().Element(ComposeHeader);
                     page.Content().Element(ComposeContent);


                     page.Footer().AlignCenter().Text(x =>
                     {
                         x.CurrentPageNumber();
                         x.Span(" / ");
                         x.TotalPages();
                     });
                 });
             });
            //doc.GeneratePdf(filename);
            //doc.GeneratePdfAndShow();
            return doc;
        }

        void ComposeHeader(IContainer container)
        {
            var titleStyle = TextStyle.Default.FontSize(20).SemiBold().FontColor(Colors.Blue.Medium);

            container.Row(row =>
            {
                row.RelativeItem().Column(column =>
                {
                    column.Item().Text($"订餐行程单").Style(titleStyle);

                    column.Item().Text(text =>
                    {
                        text.Span("打印时间: ").SemiBold();
                        text.Span(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
                    });

                    column.Item().Text(text =>
                    {
                        text.Span("打印来源: ").SemiBold();
                        text.Span($"https://groupmeals.com/order");
                    });
                });
                try
                {

                    //row.ConstantItem(100).Height(50).Placeholder();//.Image("https://wiiyaimage.blob.core.windows.net/images/logoiconmini.png");//.Placeholder();
                }
                catch (System.Exception ex)
                {
                }
            });
        }

        void ComposeContent(IContainer container)
        {
            container.PaddingVertical(40).Column(column =>
            {
                column.Spacing(5);
                for (int i = 0; i < _PDFData.Count; i++)
                {
                    var item = _PDFData[i];
                    column.Item().Row(row =>
                    {
                        row.RelativeItem().Component(new AddressComponent("就餐信息", item.OrderInfo));
                        row.ConstantItem(10);
                        row.RelativeItem().Component(new AddressComponent("餐厅位置", item.RestaurantInfo));
                        row.ConstantItem(10);
                        row.RelativeItem().Component(new AddressComponent("用餐要求", item.MealInfo));
                    });
                    if (i < _PDFData.Count - 1)
                    {
                        column.Spacing(20);
                        column.Item().PaddingVertical(5).LineHorizontal(1).LineColor(Colors.Grey.Lighten3);
                        column.Spacing(25);
                    }
                }

            });
        }
    }
    public class AddressComponent : IComponent
    {
        private string Title { get; }
        private string Address { get; }

        public AddressComponent(string title, string address)
        {
            Title = title;
            Address = address;
        }

        public void Compose(IContainer container)
        {
            container.Column(column =>
            {
                column.Spacing(2);

                //column.Item().BorderBottom(1).PaddingBottom(5).Text(Title).SemiBold();

                column.Item().Text(Address);
            });
        }
    }
    public class PDFModel
    {
        public string OrderInfo { get; set; }
        public string RestaurantInfo { get; set; }
        public string MealInfo { get; set; }
    }
}

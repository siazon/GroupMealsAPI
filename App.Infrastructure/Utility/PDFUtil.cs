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
using static System.Net.Mime.MediaTypeNames;

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
                        row.RelativeItem().Component(new OrderComponent("就餐信息", item));
                        row.ConstantItem(10);
                        row.RelativeItem().Component(new RestuarantComponent("餐厅位置", item));
                        row.ConstantItem(10);
                        row.RelativeItem().Component(new MealComponent("用餐要求", item));
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
    public class OrderComponent : IComponent
    {
        private string Title { get; }
        private PDFModel _model { get; }

        public OrderComponent(string title, PDFModel model)
        {
            Title = title;
            _model = model;
        }

        public void Compose(IContainer container)
        {
            container.ShowEntire().Column(column =>
            {
                column.Spacing(2);

                //column.Item().BorderBottom(1).PaddingBottom(5).Text(Title).SemiBold();

                column.Item().Text(text => { text.Span("订单号：").Bold(); });
                column.Item().Text(_model.BookingRef);
                column.Item().Text(text => { text.Span("时间：").Bold(); text.Span(_model.BookingTime).Bold(); });
                column.Item().Text(text => { text.Span("餐厅名称：").Bold(); text.Span(_model.RestuarantName); });
                column.Item().Text(text => { text.Span("地址：").Bold(); text.Span(_model.Address); });

            });
        }
    }
    public class RestuarantComponent : IComponent
    {
        private string Title { get; }
        private PDFModel _model { get; }

        public RestuarantComponent(string title, PDFModel model)
        {
            Title = title;
            _model = model;
        }

        public void Compose(IContainer container)
        {
            container.ShowEntire().Column(column =>
            {
                column.Spacing(2);

                //column.Item().BorderBottom(1).PaddingBottom(5).Text(Title).SemiBold();


                column.Item().Text(text => { text.Span("电话：").Bold(); text.Span(_model.Phone); });
                column.Item().Text(text => { text.Span("紧急电话：").Bold(); text.Span(_model.ContactPhone); });
                column.Item().Text(text => { text.Span("邮箱：").Bold(); text.Span(_model.Email); });
                column.Item().Text(text => { text.Span("微信：").Bold(); text.Span(_model.Wechat); });
            });
        }
    }
    public class MealComponent : IComponent
    {
        private string Title { get; }
        private PDFModel _model { get; }

        public MealComponent(string title, PDFModel model)
        {
            Title = title;
            _model = model;
        }

        public void Compose(IContainer container)
        {
            container.ShowEntire().Column(column =>
            {
                column.Spacing(2);

                //column.Item().BorderBottom(1).PaddingBottom(5).Text(Title).SemiBold();


                column.Item().Text(_model.mealInfo);
                column.Item().Text(_model.Remark);
            });
        }
    }
    public class PDFModel
    {
        public string mealInfo { get; set; }
        public string BookingRef { get; set; }
        public string BookingTime { get; set; }    
        public string RestuarantName { get; set; }
        public string Address { get; set; }    
        public string Phone { get; set; }
        public string Email { get; set; }
        public string ContactPhone { get; set; }
        public string Wechat { get; set; }
        public string Remark { get; set; }

    }
  
}

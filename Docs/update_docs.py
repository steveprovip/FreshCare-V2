import win32com.client
import os
import time

def update_word():
    print("Updating Word...")
    word = win32com.client.Dispatch("Word.Application")
    word.Visible = False
    
    doc_path = os.path.abspath("BaoCaoCongNghePhanMem_Nhom8_KTKN1_N08_V11_Final.docx")
    doc = word.Documents.Open(doc_path)
    
    replacements = {
        "<= 3 ngày": "<= 14 ngày",
        "< 3 ngày": "<= 14 ngày",
        "<= 3": "<= 14"
    }
    
    try:
        # Apply font properties generally
        doc.Content.Font.Name = "Times New Roman"
        
        # Text replacement using Find
        for search_text, replace_text in replacements.items():
            word.Selection.HomeKey(Unit=6) # wdStory = 6
            find = word.Selection.Find
            find.ClearFormatting()
            find.Replacement.ClearFormatting()
            # Wrap = wdFindContinue = 1, Replace = wdReplaceAll = 2
            find.Execute(search_text, False, False, False, False, False, True, 1, False, replace_text, 2)

        # Page Setup
        # wdPaperA4 = 7
        doc.PageSetup.PaperSize = 7
        doc.PageSetup.TopMargin = word.CentimetersToPoints(2)
        doc.PageSetup.BottomMargin = word.CentimetersToPoints(2)
        doc.PageSetup.LeftMargin = word.CentimetersToPoints(3)
        doc.PageSetup.RightMargin = word.CentimetersToPoints(1.5)
        
        # Styles (Normal)
        # wdAlignParagraphJustify = 3
        # wdLineSpace1pt5 = 1
        normal_style = doc.Styles("Normal")
        normal_style.Font.Name = "Times New Roman"
        normal_style.Font.Size = 14
        normal_style.ParagraphFormat.Alignment = 3
        normal_style.ParagraphFormat.LineSpacingRule = 1
        
        # Ensure TOC is updated
        if doc.TablesOfContents.Count > 0:
            doc.TablesOfContents.Item(1).Update()
            
    finally:
        doc.Save()
        time.sleep(1)
        doc.Close()
        word.Quit()
        print("Word updated.")

def update_ppt():
    print("Updating PPT...")
    ppt = win32com.client.DispatchEx("PowerPoint.Application")
    ppt.Visible = True # PowerPoint sometimes rejects hidden execution for opening files, we can show it but minimize it
    
    ppt_path = os.path.abspath(r"..\Slide báo cáo CNPT - Nhóm 8.pptx")
    # WithWindow=msoFalse parameter works sometimes but fails otherwise. We'll use defaults.
    pres = ppt.Presentations.Open(ppt_path)
    
    replacements = {
        "<= 3 ngày": "<= 14 ngày",
        "< 3 ngày": "<= 14 ngày",
        "<= 3": "<= 14"
    }

    try:
        for slide in pres.Slides:
            for shape in slide.Shapes:
                if shape.HasTextFrame:
                    if shape.TextFrame.HasText:
                        txt_frame = shape.TextFrame
                        txt_rng = txt_frame.TextRange
                        
                        # simple replace
                        text = txt_rng.Text
                        changed = False
                        for search_text, replace_text in replacements.items():
                            if search_text in text:
                                text = text.replace(search_text, replace_text)
                                changed = True
                        if changed:
                            txt_rng.Text = text
    finally:
        pres.Save()
        time.sleep(1)
        pres.Close()
        ppt.Quit()
        print("PPT updated.")

if __name__ == "__main__":
    update_word()
    update_ppt()

   M S B A M L   `   `   `  ����F  BPC_C_Client, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null)clr-namespace:PC_C_ClientPC_C_Client  5   	       PC_C_Client.MainWindow   5   	   R NWindowsBase, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35\ XPresentationFramework, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35W SPresentationCore, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35D 9http://schemas.microsoft.com/winfx/2006/xaml/presentation    5   	   R NSystem.Xaml, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089:x,http://schemas.microsoft.com/winfx/2006/xaml     5   	   @d2http://schemas.microsoft.com/expression/blend/2008     5   	   Jmc;http://schemas.openxmlformats.org/markup-compatibility/2006     5   	   +localclr-namespace:PC_C_Client     5   	   -   5          � Title$  Klients��5   	   $��40��6   $	��150��6%    � ShowInTaskbar .   � WindowStyle$
 None=�6G    � WindowState$ 	Maximized=�6Z    � 
ResizeMode$ NoResize=�6r    � AllowsTransparency . 
������ .��5
      � -   5         � ColumnDefinitions 5   
   �� 5      $��5* �6   �� 5      $	��73* �6   �� 5      $	��72* �6   .+�5   
   �� -   5        $��LockScreenImage��5      ���#d7f067$y�	Collapsed=�6:   $��3��6Q   �� 5   
   -   5        $��TimerRectangle��5      ���	#FFC8C6C6$	��150��6;   $��40��6G   $��Center=�5      $	��Top=�61   $��0,0,0,0q�6I   $y�	Collapsed=�6Z   $��3��6q   �� 5   
   -   5        $��TimerTextBlock��5      $��Center=�6*   $	��Top=�6G   $��5,5,6,0q�5      $��20(�6%   
���   �$y�	Collapsed=�6G   $��1��6^   $��31��6n   $��2��6z   $	��134��6�   �� 5   
   -   5        $��bibliotekasLogo��5      $��Center=�6(   $	��540��6E   $��-412,-250,-438,-250q�6R   $��Center=�6o   $
��1000��6�   $��Parlielupe_Flat.png��6�   $y�	Collapsed=�6�   $��3��6�   �� 5   
   -   5        $��EndTimerRectangle��5      ���#d7f067$	��841��6<   $	��100��6H   $��Center=�5      $��Center=�61   $��-342,-30,-349,-30q�6L   $��3��6h   �� 5   
   -   5        $��EndTimeTextBlock��5      $��Center=�6,   $��Center=�6I   $��-295,-28,-332,-32q�5      $��50(�6/   
���   �$	��777��6P   $��3��6\   $	��100��6p   �� 5   
   -	   5        $��PazinojumaText��5      $��Center=�6*   $��Center=�6G   $��-231,-18,-396,-42q�5      $��50(�6/   
���   �$	��777��6P   $y�	Collapsed=�6\   $��3��6s   $	��100��6�   �� 5   
   -
   5        $��LaiksBeidzas��5      $��Center=�6(   $��Center=�6E   $��-329,0,-335,-60q�5       $��27(�6-   
���   �$	��814��6N   $��3��6Z   $	��100��6n   $M��GJūsu laiks beidzas! Lūdzu aizveriet programmas un atbrīvojiet datoru��6{   �� 5!   
   -   5        $��PazinojumaButton��5!      $
��Labi��6)   $��2��68   $
��Left=�6H   $��30��6c   $��316,34,-305,-24q�6o   $��Center=�6�   $��61��6�   $��16(�6�   6�   �� 5"   
   -   5        $��AdminButton��5"      $��Admin��6$   $��2��64   $��Right=�6D   $��43��6`   $��0,0,0,0q�6l   $��Bottom=�6}   $��14(�6�   $��98��6�   6�   5#      5$      
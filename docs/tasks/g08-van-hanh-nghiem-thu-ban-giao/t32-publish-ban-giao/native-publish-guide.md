# T32 — Kiểm bản publish ứng viên

**Đợt test G08 đã được duyệt và chạy; kết quả/giới hạn tại [test-results.md](test-results.md).** Dùng output của Publish-G08.ps1 và release-manifest.json có ArchiveHash. Giải nén candidate.zip sang thư mục mới artifacts/g08/<runID>/run-publish; xác minh file/hash theo package-manifest.json (không tự tính manifest làm bằng chứng của chính nó).

1. Ghi OS/x64, Desktop Runtime từ dotnet --list-runtimes, executable hash, schema/DB test, version/checksum. Chạy exe từ thư mục mới không mởVS. Máy khác chưa thử ghiNOT RUN. Framework-dependent phải chạy với Runtime thật, không chỉ build; self-contained không chọn và ghiN/A có lýdo.
2. T32-K03/K07: làm lại clean install/bootstrap bằng Windows setup riêng và revoke, login runtime identity riêng. Không admin mặc định; thiếuconfig/saiDB/chứngchỉlỗi khôngfallback. NhánhDemo seedduynhấttrướcmigrations, account/hash theo operator. Chạy bán/in theoN01;previewkhôngthay in giấy.
3. T32-K05/K09: native5luồng theo T31nativeguide vàdocs/operations/demo.md trên chính exe này, đốichiếu SQL trực tiếp. Mọi evidence ghiPackageHash, không dùngDebugprobe thay hànhvi UIpackage.
4. T32-K06: backup/restoreT30 vàoDBtestmới, mapruntimeuserđúnglogin, đổi connection của tiếntrìnhcon sangDBrestore; không sửa source/package. Login/report/smoke đúngdữliệu; ghi snapshot trước/sau và version.
5. T32-K04/K08: inventory audit khôngsecret/DPAPI/backup/dữliệuriêng/đườngdẫnmáytácgiả; đọc báo cáo/knownlimitations đúng trạngthái325ID. Licenses còntrongResources, khôngtuyênbốPASS từquétkeyword.
6. Gatebypass: source/launcher/profile/configvàDLLDebug/Release/publishkhônghelper/flag cũ;positiveLoginForm/AuthService/AuthRepositorycòn. Đặt các biếncũ trong tiếntrìnhcon theo checklistlịch sử (khôngsửaenvtoànmáy), chạy lại vẫnLogin;khôngsessiontựtạo. Thửloginđúng/sai/logout/relogin/khóa/đổirole/nghỉviệc/thu hồi phiênvàBLL3role. Ghi mỗi subcase actual, khônglấy auditDLLthaynative.
7. Đóng tiếntrình, khôi phục envphiên điều khiển nếuđãđặt, dọnDBtest/filetạm exactownership và báogiữbackup/packagecóchủđích. KhôngxóaDBdevelopment. Tấtcảkếtquảtest-results.md/reportG08; nếu phát hiệnlỗi, lưuFAIL và trìnhđợtbug, khôngpatchpackagegiữalượt.

HìnhthứcSKIPPEDtheophâncông;125%chức năng/layoutkỹthuật,100/150%NOTRUN. Chưa kiểmđủ thì packagevẫn“ứngviên”,G08Đanglàm.


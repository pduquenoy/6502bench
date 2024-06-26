        !cpu    6502
THREE_K =       $3000      ;project symbol test
IN_1    =       $9000
IN_2    =       $a000
IN_3    =       $b000
IN_4    =       $c000

*       =       $0800
        jsr     region1
        jsr     region2
        jsr     THREE_K
        jsr     region4    ;operand label set manually
        lda     L9005
        lda     LA008
        lda     IN_3+11
        lda     IN_4+14
self    !byte   $ad,$ea
L081A   nop
        jsr     altbnk1
        jsr     altbnk2
        jmp     done

altbnk1 bit     $ffc0
        lda     self+1
        bne     L081A
        rts

        !pseudopc *+$0000 {
altbnk2 bit     $ffc0
        lda     $0819
        bne     $081a
        ldx     $081b
        rts

        }
done    nop
        rts

        !fill   197,$00

        !pseudopc $1000 {
region1x lda    region1x
        nop
        nop
        nop
        nop
        nop
        nop
        nop
        nop
        nop
        nop
        nop
        nop
        nop
        nop
        nop
        nop
        nop
        nop
        nop
        nop
        nop
        nop
        nop
        nop
        nop
        nop
        nop
        nop
        nop
        nop
        nop
        nop
        nop
        nop
        nop
        nop
        nop
        nop
        nop
        nop
        nop
        nop
        nop
        nop
        nop
        nop
        nop
        nop
        nop
        nop
        rts

        !fill   202,$00
        }

        !pseudopc $1000 {
region1 lda     region1
        pha
        ldy     #$11
        nop
@copy   lda     inner1_pre,y
        sta     inner1,y
        dey
        bpl     @copy
        bit     $101d
        jsr     inner1
        jmp     finish1

inner1_pre
        !pseudopc $9000 {
inner1  ldx     inner1
        ldy     #$aa
L9005   ldy     finish1
        ldy     finish2
        ldy     $302b
        ldy     $402b
        rts

        }
finish1 ldy     finish1
        ldx     region1
        ldx     region2
        ldx     THREE_K
        ldx     $4000
        lda     inner1
        lda     inner2
        lda     IN_3
        lda     IN_4
        pla
        rts

        !fill   184,$00
        }

        !pseudopc $2000 {
region2 lda     region2
        pha
        ldy     #$11
        nop
@copy   lda     inner2_pre,y
        sta     inner2,y
        dey
        bpl     @copy
        bit     $201d
        jsr     inner2
        jmp     finish2

inner2_pre
        !pseudopc $a000 {
inner2  ldx     inner2
        ldy     #$aa
        ldy     $102b
LA008   ldy     finish2
        ldy     $302b
        ldy     $402b
        rts

        }
finish2 ldy     finish2
        ldx     $1000
        ldx     region2
        ldx     THREE_K
        ldx     $4000
        lda     IN_1
        lda     inner2
        lda     IN_3
        lda     IN_4
        pla
        rts

        !fill   184,$00
        }

        !pseudopc $3000 {
region3 lda     region3
        pha
        ldy     #$11
        nop
@copy   lda     inner3_pre,y
        sta     inner3,y
        dey
        bpl     @copy
        bit     $301d
        jsr     inner3
        jmp     finish3

inner3_pre
        !pseudopc $b000 {
inner3  ldx     inner3
        ldy     #$aa
        ldy     finish1
        ldy     finish2
        ldy     finish3
        ldy     $402b
        rts

        }
finish3 ldy     finish3
        ldx     region1
        ldx     region2
        ldx     region3
        ldx     $4000
        lda     inner1
        lda     inner2
        lda     inner3
        lda     IN_4
        pla
        rts

        !fill   184,$00
        }

        !pseudopc $4000 {
region4 lda     region4
        pha
        ldy     #$11
        nop
@copy   lda     inner4_pre,y
        sta     inner4,y
        dey
        bpl     @copy
        bit     $401d
        jsr     inner4
        jmp     finish4

inner4_pre
        !pseudopc $c000 {
inner4  ldx     inner4
        ldy     #$aa
        ldy     $102b
        ldy     $202b
        ldy     $302b
_checkit ldy    finish4
        rts

        }
finish4 ldy     finish4
        ldx     $1000
        ldx     $2000
        ldx     THREE_K
        ldx     region4
        lda     IN_1
        lda     IN_2
        lda     IN_3
        lda     inner4
        pla
        rts

        !fill   184,$00
        }
        !pseudopc $0000 {
        !byte   $ff
        }
